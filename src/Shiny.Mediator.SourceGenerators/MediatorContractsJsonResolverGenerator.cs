using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;


/// <summary>
/// Emits a per-assembly <see cref="System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver"/>
/// covering every contract type that a registered handler (discovered by
/// <see cref="MediatorSourceGenerator"/>) consumes or produces — request types, response types,
/// command types, event types, stream-request element types, and the common collection wrappers
/// (<c>List&lt;T&gt;</c>, <c>T[]</c>, <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>,
/// <c>IList&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>IAsyncEnumerable&lt;T&gt;</c>) of every element type — plus a <c>[ModuleInitializer]</c> that
/// registers it with <c>Shiny.Json</c>.
///
/// <para>
/// Per-type <see cref="System.Text.Json.Serialization.JsonConverter{T}"/> implementations are
/// generated on demand via <see cref="JsonConverterSourceGenerator"/> in attach-attribute-off mode
/// so users don't have to mark every contract type <c>partial</c>. Types that already carry a
/// <c>[JsonConverter(typeof(X))]</c> attribute (e.g. OpenAPI- or [SourceGenerateJsonConverter]-
/// emitted ones) reuse the existing converter type; nothing is double-emitted.
/// </para>
///
/// <para>
/// Element types declared in referenced assemblies are skipped — they are the responsibility of
/// their owning assembly's resolver. Collection wrappers reference element <c>JsonTypeInfo</c>
/// through the chain, so cross-assembly composition still works as long as the source assembly
/// registers its types.
/// </para>
/// </summary>
internal static class MediatorContractsJsonResolverGenerator
{
    public static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyCollection<(ITypeSymbol RequestType, ITypeSymbol? ResultType)> handlerPairs,
        string namespaceName
    )
    {
        // BFS over contract types and (for in-assembly types) their public property types.
        // Transitive walking matters: a handler returning RideTime should pull in RideTime's Position
        // property type too, otherwise the per-type converter for RideTime fails at runtime when it
        // hits the Position property fallback. Walk stops at assembly boundaries, primitives, and
        // types that already declare their own [JsonConverter] (we trust those to be self-contained).
        var entries = new Dictionary<string, ContractEntry>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<ITypeSymbol>();

        foreach (var pair in handlerPairs)
        {
            Enqueue(pair.RequestType, visited, queue);
            if (pair.ResultType is not null)
                Enqueue(pair.ResultType, visited, queue);
        }

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            var entry = MaterializeEntry(type, compilation);
            if (entry is null)
                continue;

            entries[entry.FullyQualifiedTypeName] = entry;

            // Transitive: walk public properties for in-assembly types so RideTime → Position →
            // (and anything Position references) all land in the resolver.
            if (entry.IsInCurrentAssembly && entry.NamedTypeSymbol is not null)
            {
                foreach (var prop in entry.NamedTypeSymbol.GetMembers().OfType<IPropertySymbol>()
                             .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod is not null))
                {
                    Enqueue(prop.Type, visited, queue);
                }
            }
        }

        if (entries.Count == 0)
            return;

        // Emit per-type JsonConverter for in-assembly types that don't already have one. Dedupe
        // across handlers (multiple handlers can reference the same contract type).
        var generated = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries.Values)
        {
            if (!entry.IsInCurrentAssembly || !entry.NeedsGeneratedConverter)
                continue;
            if (!generated.Add(entry.FullyQualifiedTypeName))
                continue;

            JsonConverterSourceGenerator.GenerateJsonConverter(context, entry.NamedTypeSymbol!, attachAttribute: false);
        }

        EmitResolver(context, namespaceName, entries.Values);
    }


    static void Enqueue(ITypeSymbol type, HashSet<string> visited, Queue<ITypeSymbol> queue)
    {
        var element = ExtractElementType(type);
        if (element is null || !ShouldRegister(element))
            return;

        var fqn = FullName(element);
        if (visited.Add(fqn))
            queue.Enqueue(element);
    }


    static ContractEntry? MaterializeEntry(ITypeSymbol type, Compilation compilation)
    {
        var element = ExtractElementType(type);
        if (element is null || !ShouldRegister(element))
            return null;

        var fqn = FullName(element);

        // Discover an existing per-type JsonConverter. The user could have a hand-written
        // [JsonConverter(typeof(X))] or the [SourceGenerateJsonConverter] attribute (which causes
        // JsonConverterSourceGenerator to emit a partial declaration with [JsonConverter] wired).
        var existingConverter = FindJsonConverter(element);
        var hasSourceGenAttr = HasAttribute(element, "SourceGenerateJsonConverterAttribute");
        var isInCurrentAssembly = SymbolEqualityComparer.Default.Equals(element.ContainingAssembly, compilation.Assembly);
        var named = element as INamedTypeSymbol;

        string converterTypeRef;
        bool needsGeneratedConverter;

        if (existingConverter is not null)
        {
            converterTypeRef = FullName(existingConverter);
            needsGeneratedConverter = false;
        }
        else if (hasSourceGenAttr || (isInCurrentAssembly && named is not null))
        {
            // Use the conventional {Namespace}.{TypeName}JsonConverter name. For
            // [SourceGenerateJsonConverter] types the converter is emitted by the existing pipeline
            // and we must NOT regenerate it here (would collide). For other in-assembly types we'll
            // emit it ourselves via GenerateJsonConverter(attachAttribute: false).
            var ns = element.ContainingNamespace?.ToDisplayString();
            converterTypeRef = $"global::{(string.IsNullOrEmpty(ns) ? "" : ns + ".")}{element.Name}JsonConverter";
            needsGeneratedConverter = !hasSourceGenAttr;
        }
        else
        {
            // Cross-assembly type without [JsonConverter] / [SourceGenerateJsonConverter] — we don't
            // emit an element entry for it. Collection wrappers still get registered and will look
            // up the element TypeInfo through the chain at runtime.
            converterTypeRef = string.Empty;
            needsGeneratedConverter = false;
        }

        return new ContractEntry(
            FullyQualifiedTypeName: fqn,
            ConverterTypeFullyQualifiedName: converterTypeRef,
            IsInCurrentAssembly: isInCurrentAssembly,
            NeedsGeneratedConverter: needsGeneratedConverter,
            NamedTypeSymbol: named
        );
    }


    static ITypeSymbol? ExtractElementType(ITypeSymbol type)
    {
        // Unwrap collection wrappers down to their element type so the same set of registrations
        // covers single + collection-shaped handler signatures.
        switch (type)
        {
            case IArrayTypeSymbol arr:
                return ExtractElementType(arr.ElementType);

            case INamedTypeSymbol named when named.IsGenericType && IsCollectionLikeInterface(named):
                return named.TypeArguments.Length == 1 ? ExtractElementType(named.TypeArguments[0]) : null;

            case INamedTypeSymbol named when named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T:
                return named.TypeArguments.Length == 1 ? ExtractElementType(named.TypeArguments[0]) : null;

            default:
                return type;
        }
    }


    static bool IsCollectionLikeInterface(INamedTypeSymbol named)
    {
        var def = named.ConstructedFrom.ToDisplayString();
        return def is "System.Collections.Generic.List<T>"
            or "System.Collections.Generic.IList<T>"
            or "System.Collections.Generic.ICollection<T>"
            or "System.Collections.Generic.IReadOnlyList<T>"
            or "System.Collections.Generic.IReadOnlyCollection<T>"
            or "System.Collections.Generic.IEnumerable<T>"
            or "System.Collections.Generic.IAsyncEnumerable<T>";
    }


    static bool ShouldRegister(ITypeSymbol type)
    {
        // Skip primitives, system types, Task/Unit/void, etc. STJ either handles them natively or
        // they aren't ours to register.
        if (type is null) return false;
        if (type.SpecialType != SpecialType.None) return false; // string, int, bool, object, etc.
        if (type.TypeKind == TypeKind.Enum) return false; // enums need different wiring; skip in v1
        if (type.TypeKind == TypeKind.Interface) return false; // can't generate converter for arbitrary interfaces

        var fqn = FullName(type);
        if (fqn.StartsWith("global::System.", StringComparison.Ordinal)) return false;
        if (fqn.StartsWith("global::Microsoft.", StringComparison.Ordinal)) return false;

        // Need a NamedTypeSymbol so we can introspect attributes and generate a converter.
        return type is INamedTypeSymbol;
    }


    static INamedTypeSymbol? FindJsonConverter(ITypeSymbol type)
    {
        var attr = type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonConverterAttribute");

        if (attr is null) return null;
        if (attr.ConstructorArguments.Length == 0) return null;
        return attr.ConstructorArguments[0].Value as INamedTypeSymbol;
    }


    static bool HasAttribute(ITypeSymbol type, string attributeName)
        => type.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);


    static string FullName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));


    static void EmitResolver(
        SourceProductionContext context,
        string namespaceName,
        IEnumerable<ContractEntry> entries
    )
    {
        var ordered = entries.OrderBy(e => e.FullyQualifiedTypeName, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0)
            return;

        var resolverName = "__ShinyMediatorContractsJsonResolver";
        var initName = resolverName + "Init";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"internal sealed class {resolverName} : IJsonTypeInfoResolver");
        sb.AppendLine("{");
        sb.AppendLine($"    internal static readonly {resolverName} Instance = new();");
        sb.AppendLine();
        sb.AppendLine("    public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)");
        sb.AppendLine("    {");

        // Element entries — only for types where we have a converter (in-assembly + generated, or
        // any assembly with an existing [JsonConverter] attribute).
        foreach (var e in ordered)
        {
            if (string.IsNullOrEmpty(e.ConverterTypeFullyQualifiedName))
                continue;
            sb.AppendLine($"        if (type == typeof({e.FullyQualifiedTypeName}))");
            sb.AppendLine($"            return JsonMetadataServices.CreateValueInfo<{e.FullyQualifiedTypeName}>(options, new {e.ConverterTypeFullyQualifiedName}());");
        }

        // Collection wrappers — registered for every element type we know about, regardless of
        // whether we own the element TypeInfo (cross-assembly elements resolve through the chain).
        foreach (var e in ordered)
        {
            var t = e.FullyQualifiedTypeName;
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.List<{t}>))");
            sb.AppendLine($"            return CreateList<{t}>(options);");
            sb.AppendLine($"        if (type == typeof({t}[]))");
            sb.AppendLine($"            return CreateArray<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.IEnumerable<{t}>))");
            sb.AppendLine($"            return CreateEnumerable<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.IList<{t}>))");
            sb.AppendLine($"            return CreateIList<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.ICollection<{t}>))");
            sb.AppendLine($"            return CreateICollection<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.IReadOnlyList<{t}>))");
            sb.AppendLine($"            return CreateReadOnlyList<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.IReadOnlyCollection<{t}>))");
            sb.AppendLine($"            return CreateReadOnlyCollection<{t}>(options);");
            sb.AppendLine($"        if (type == typeof(global::System.Collections.Generic.IAsyncEnumerable<{t}>))");
            sb.AppendLine($"            return CreateAsyncEnumerable<{t}>(options);");
        }

        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        AppendCollectionHelpers(sb);
        AppendReadOnlyConverters(sb);
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"internal static class {initName}");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine($"    internal static void Init() => global::Shiny.Json.AddResolver({resolverName}.Instance);");
        sb.AppendLine("}");

        context.AddSource($"{namespaceName}.{resolverName}.g.cs", sb.ToString());
    }


    static void AppendCollectionHelpers(StringBuilder sb)
    {
        sb.AppendLine("    static JsonTypeInfo CreateList<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateListInfo<List<T>, T>(options, new JsonCollectionInfoValues<List<T>> { ObjectCreator = static () => new List<T>(), ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateArray<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateArrayInfo<T>(options, new JsonCollectionInfoValues<T[]> { ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateEnumerable<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateIEnumerableInfo<IEnumerable<T>, T>(options, new JsonCollectionInfoValues<IEnumerable<T>> { ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateIList<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateIListInfo<IList<T>, T>(options, new JsonCollectionInfoValues<IList<T>> { ObjectCreator = static () => new List<T>(), ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateICollection<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateICollectionInfo<ICollection<T>, T>(options, new JsonCollectionInfoValues<ICollection<T>> { ObjectCreator = static () => new List<T>(), ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateAsyncEnumerable<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateIAsyncEnumerableInfo<IAsyncEnumerable<T>, T>(options, new JsonCollectionInfoValues<IAsyncEnumerable<T>> { ElementInfo = elem });");
        sb.AppendLine("    }");
        sb.AppendLine();
        // JsonMetadataServices has no factory for IReadOnlyList/IReadOnlyCollection — use custom
        // converters that round-trip through List<T>. Same pattern Shiny.Extensions.Serialization
        // uses for the [ShinyJsonInclude] resolver.
        sb.AppendLine("    static JsonTypeInfo CreateReadOnlyList<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateValueInfo<IReadOnlyList<T>>(options, new ReadOnlyListConverter<T>(elem));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo CreateReadOnlyCollection<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        var elem = ResolveElement<T>(options); if (elem is null) return null;");
        sb.AppendLine("        return JsonMetadataServices.CreateValueInfo<IReadOnlyCollection<T>>(options, new ReadOnlyCollectionConverter<T>(elem));");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static JsonTypeInfo<T> ResolveElement<T>(JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        try { return options.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>; }");
        sb.AppendLine("        catch (System.InvalidOperationException) { return null; }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }


    static void AppendReadOnlyConverters(StringBuilder sb)
    {
        sb.AppendLine("    sealed class ReadOnlyListConverter<T> : JsonConverter<IReadOnlyList<T>>");
        sb.AppendLine("    {");
        sb.AppendLine("        readonly JsonTypeInfo<T> elementInfo;");
        sb.AppendLine("        public ReadOnlyListConverter(JsonTypeInfo<T> elementInfo) => this.elementInfo = elementInfo;");
        sb.AppendLine("        public override IReadOnlyList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == JsonTokenType.Null) return null;");
        sb.AppendLine("            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();");
        sb.AppendLine("            var list = new List<T>();");
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType == JsonTokenType.EndArray) return list;");
        sb.AppendLine("                list.Add(JsonSerializer.Deserialize(ref reader, this.elementInfo));");
        sb.AppendLine("            }");
        sb.AppendLine("            throw new JsonException();");
        sb.AppendLine("        }");
        sb.AppendLine("        public override void Write(Utf8JsonWriter writer, IReadOnlyList<T> value, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteStartArray();");
        sb.AppendLine("            for (int i = 0; i < value.Count; i++) JsonSerializer.Serialize(writer, value[i], this.elementInfo);");
        sb.AppendLine("            writer.WriteEndArray();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    sealed class ReadOnlyCollectionConverter<T> : JsonConverter<IReadOnlyCollection<T>>");
        sb.AppendLine("    {");
        sb.AppendLine("        readonly JsonTypeInfo<T> elementInfo;");
        sb.AppendLine("        public ReadOnlyCollectionConverter(JsonTypeInfo<T> elementInfo) => this.elementInfo = elementInfo;");
        sb.AppendLine("        public override IReadOnlyCollection<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (reader.TokenType == JsonTokenType.Null) return null;");
        sb.AppendLine("            if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();");
        sb.AppendLine("            var list = new List<T>();");
        sb.AppendLine("            while (reader.Read())");
        sb.AppendLine("            {");
        sb.AppendLine("                if (reader.TokenType == JsonTokenType.EndArray) return list;");
        sb.AppendLine("                list.Add(JsonSerializer.Deserialize(ref reader, this.elementInfo));");
        sb.AppendLine("            }");
        sb.AppendLine("            throw new JsonException();");
        sb.AppendLine("        }");
        sb.AppendLine("        public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<T> value, JsonSerializerOptions options)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteStartArray();");
        sb.AppendLine("            foreach (var item in value) JsonSerializer.Serialize(writer, item, this.elementInfo);");
        sb.AppendLine("            writer.WriteEndArray();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }


    sealed record ContractEntry(
        string FullyQualifiedTypeName,
        string ConverterTypeFullyQualifiedName,
        bool IsInCurrentAssembly,
        bool NeedsGeneratedConverter,
        INamedTypeSymbol? NamedTypeSymbol
    );
}
