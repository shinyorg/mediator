using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi;

namespace Shiny.Mediator.SourceGenerators;


public class OpenApiModelGenerator(MediatorHttpItemConfig config, SourceProductionContext context, Compilation compilation)
{
    readonly string accessor = config.UseInternalClasses ? "internal" : "public";
    readonly HashSet<string> generatedTypes = new();
    readonly HashSet<string> convertedTypes = new();
    readonly Dictionary<string, bool> enumTypes = new();
    readonly OpenApiPolymorphism polymorphism = new();
    readonly List<(string Name, IOpenApiSchema Schema)> pendingConverters = new();
    readonly HashSet<string> collectionElementTypes = new(StringComparer.Ordinal);

    public MediatorHttpItemConfig Config => config;
    public SourceProductionContext Context => context;
    public Compilation Compilation { get; private set; } = compilation;

    /// <summary>Names (unqualified) of every type the model generator has emitted for this item.</summary>
    public IReadOnlyCollection<string> GeneratedTypeNames => this.generatedTypes;

    /// <summary>
    /// Fully-qualified names (without the leading <c>global::</c>) of every type for which a
    /// per-type <see cref="System.Text.Json.Serialization.JsonConverter{T}"/> was emitted via
    /// <see cref="JsonConverterSourceGenerator.GenerateJsonConverter"/>. The OpenAPI resolver
    /// emitter consumes this set to know which types it can wire via
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateValueInfo{T}(System.Text.Json.JsonSerializerOptions,System.Text.Json.Serialization.JsonConverter)"/>.
    /// </summary>
    public ISet<string> ConvertedTypeNames => this.convertedTypes;

    /// <summary>
    /// Map of FQN → isStringEnum for every enum the model generator has emitted. The OpenAPI
    /// resolver emitter uses this to register enum types with the right converter
    /// (<see cref="System.Text.Json.Serialization.JsonStringEnumConverter{T}"/> for string enums,
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonMetadataServices.GetEnumConverter{T}"/>
    /// for numeric enums) and to wire their <c>Nullable&lt;TEnum&gt;</c> counterparts.
    /// </summary>
    public IDictionary<string, bool> EnumTypeNames => this.enumTypes;
    public IReadOnlyCollection<string> CollectionElementTypeNames => collectionElementTypes;

    public void UpdateCompilation(Compilation newCompilation) => Compilation = newCompilation;

    public void PreparePolymorphism(IDictionary<string, IOpenApiSchema> schemas)
    {
        if (config.GeneratePolymorphicModels)
            polymorphism.Analyze(schemas, config.GenerateJsonConverters);
    }

    public void CompleteModels()
    {
        // Symbols for every component must exist before inspecting derived/inherited or forward
        // referenced properties. Otherwise a valid wire property can be mistaken for an error type.
        foreach (var pending in pendingConverters)
            GenerateConverter(pending.Name, pending.Schema);
        pendingConverters.Clear();
    }


    public void GenerateModel(string name, IOpenApiSchema schema)
    {
        var className = name.Pascalize().ToSafeIdentifier();
        if (config.GeneratePolymorphicModels && !polymorphism.Bases.ContainsKey(className))
            OpenApiPolymorphism.RejectInlineDiscriminator(schema);

        if (this.generatedTypes.Add(className))
        {
            if (IsGeneratableEnum(schema))
            {
                GenerateEnum(className, schema);
            }
            else if (schema.Type?.HasFlag(JsonSchemaType.Object) == true || schema.Properties?.Count > 0 || polymorphism.Bases.ContainsKey(className))
            {
                GenerateClass(className, schema);
            }
        }
    }


    /// <summary>
    /// Only string and integral enums map to a C# enum. Anything else (e.g. boolean "const" style
    /// enums like <c>type: boolean, enum: [false]</c>) falls back to the schema's plain type.
    /// </summary>
    static bool IsGeneratableEnum(IOpenApiSchema schema)
    {
        var values = schema.Enum?.Where(x => x != null).ToList();
        if (values == null || values.Count == 0)
            return false;

        if (values.All(x => x!.GetValueKind() == System.Text.Json.JsonValueKind.String))
            return true;

        return values.All(x => TryGetEnumInt(x!, out _));
    }


    // YAML documents surface numbers as decimal-backed JsonValues, so GetValue<int>() throws - parse the raw json instead
    static bool TryGetEnumInt(System.Text.Json.Nodes.JsonNode node, out long value)
    {
        value = 0;
        if (node.GetValueKind() != System.Text.Json.JsonValueKind.Number)
            return false;

        if (!Decimal.TryParse(node.ToJsonString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return false;

        if (d != Decimal.Truncate(d) || d < Int64.MinValue || d > Int64.MaxValue)
            return false;

        value = (long)d;
        return true;
    }


    void GenerateEnum(string enumName, IOpenApiSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();

        // Check if the first enum value is a string-based value
        var firstValue = schema.Enum?.FirstOrDefault();
        var isStringEnum = firstValue != null && firstValue.GetValueKind() == System.Text.Json.JsonValueKind.String;

        // Track the enum so the resolver emitter can wire its converter + nullable counterpart.
        this.enumTypes[$"{config.Namespace}.{enumName}"] = isStringEnum;

        if (isStringEnum)
        {
            sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof(global::System.Text.Json.Serialization.JsonStringEnumConverter<global::{config.Namespace}.{enumName}>))]");
        }
        else
        {
            // without a converter attribute, generated json converters treat enums as strings
            sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof(global::System.Text.Json.Serialization.JsonNumberEnumConverter<global::{config.Namespace}.{enumName}>))]");
            if (GetExtensionBoolean(schema, "x-enumFlags"))
                sb.AppendLine("[global::System.Flags]");
        }

        sb.AppendLine(Constants.GeneratedCodeAttributeString);

        if (isStringEnum)
        {
            sb.AppendLine($"{accessor} enum {enumName}");
            sb.AppendLine("{");
            var memberNames = new HashSet<string>();
            foreach (var ev in schema.Enum!)
            {
                if (ev != null)
                {
                    var strValue = ev.GetValue<string>();
                    var memberName = strValue;

                    // values like "Too Many Requests" aren't valid members - sanitize and map back to the wire value
                    if (!SyntaxFacts.IsValidIdentifier(strValue) || SyntaxFacts.GetKeywordKind(strValue) != SyntaxKind.None)
                    {
                        memberName = strValue.Pascalize().ToSafeIdentifier();
                        if (SyntaxFacts.GetKeywordKind(memberName) != SyntaxKind.None)
                            memberName = "_" + memberName;
                    }

                    var baseName = memberName;
                    var i = 2;
                    while (!memberNames.Add(memberName))
                        memberName = baseName + i++;

                    if (memberName != strValue)
                        sb.AppendLine($"    [global::System.Text.Json.Serialization.JsonStringEnumMemberName(\"{strValue.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")]");

                    sb.AppendLine($"    {memberName},");
                }
            }
        }
        else
        {
            var values = new List<long>();
            foreach (var ev in schema.Enum!)
            {
                if (ev != null)
                {
                    TryGetEnumInt(ev, out var value);
                    values.Add(value);
                }
            }

            // Member names come from the NSwag/AutoRest conventions (x-enumNames, x-enum-varnames)
            // when the document carries them and they line up with the values; otherwise Value{n}.
            var names = GetExtensionStrings(schema, "x-enumNames") ?? GetExtensionStrings(schema, "x-enum-varnames");
            if (names == null || names.Count != values.Count)
            {
                names = values
                    .Select(v => v < 0
                        ? "ValueMinus" + (-(decimal)v).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : "Value" + v.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .ToList();
            }

            var underlying = values.Any(v => v < Int32.MinValue || v > Int32.MaxValue) ? " : long" : String.Empty;
            sb.AppendLine($"{accessor} enum {enumName}{underlying}");
            sb.AppendLine("{");

            var used = new HashSet<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var memberName = names[i].ToSafeIdentifier();
                while (!used.Add(memberName))
                    memberName += "_";

                sb.AppendLine($"    {memberName} = {values[i].ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            }
        }

        sb.AppendLine("}");

        AddModelSource(enumName, sb.ToString());
    }
    

    static List<string>? GetExtensionStrings(IOpenApiSchema schema, string name)
    {
        if (schema.Extensions == null || !schema.Extensions.TryGetValue(name, out var extension))
            return null;

        if (extension is JsonNodeExtension { Node: System.Text.Json.Nodes.JsonArray array })
        {
            var list = new List<string>();
            foreach (var item in array)
            {
                if (item is not System.Text.Json.Nodes.JsonValue value || value.GetValueKind() != System.Text.Json.JsonValueKind.String)
                    return null;

                list.Add(value.GetValue<string>());
            }
            return list;
        }

        return null;
    }

    static bool GetExtensionBoolean(IOpenApiSchema schema, string name)
        => schema.Extensions != null
           && schema.Extensions.TryGetValue(name, out var extension)
           && extension is JsonNodeExtension { Node: System.Text.Json.Nodes.JsonValue value }
           && value.GetValueKind() == System.Text.Json.JsonValueKind.True;

    void GenerateClass(string className, IOpenApiSchema schema)
    {
        if (config.GeneratePolymorphicModels && !polymorphism.Bases.ContainsKey(className))
            OpenApiPolymorphism.RejectInlineDiscriminator(schema);
        polymorphism.Bases.TryGetValue(className, out var polymorphicBase);
        polymorphism.Variants.TryGetValue(className, out var polymorphicVariant);
        var discriminator = polymorphicBase?.Discriminator ?? polymorphicVariant?.Parent.Discriminator;
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        if (config.GeneratePolymorphicModels)
            sb.AppendLine("#nullable enable annotations");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        if (polymorphicBase != null)
            sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof({className}JsonConverter))]");
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        var modifier = polymorphicBase != null ? "abstract " : polymorphicVariant != null ? "sealed " : String.Empty;
        sb.Append($"{accessor} {modifier}partial class {className}");

        // Handle inheritance
        if (polymorphicVariant != null)
        {
            sb.AppendLine($" : global::{config.Namespace}.{polymorphicVariant.Parent.Name}");
        }
        else if (schema.AllOf?.Count == 1)
        {
            var baseType = GetSchemaType(schema.AllOf[0]);
            if (baseType != null)
            {
                sb.AppendLine($" : {baseType}");
            }
            else
            {
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine();
        }

        sb.AppendLine("{");

        // Generate properties
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                // The variant determines this value; a mutable Kind property could contradict
                // the runtime type and reintroduce ambiguous or incomplete wire payloads.
                if (property.Key == discriminator)
                    continue;
                var propertyName = property.Key.Pascalize().ToSafeIdentifier();

                if (property.Value != null)
                {
                    var propSchema = property.Value;
                    string? typeName;

                    // Check if this is a reference to a component schema first
                    if (propSchema is OpenApiSchemaReference)
                    {
                        // Use the referenced type name directly
                        typeName = GetSchemaType(propSchema);
                    }
                    // Check if this is a nested object (no title means it's inline)
                    else if (propSchema.Type?.HasFlag(JsonSchemaType.Object) == true && propSchema.Properties != null && String.IsNullOrEmpty(propSchema.Title))
                    {
                        // Generate nested type (only if not already generated)
                        typeName = className + propertyName;
                        if (generatedTypes.Add(typeName))
                        {
                            GenerateClass(typeName, propSchema);
                        }
                    }
                    else if (IsGeneratableEnum(propSchema) && String.IsNullOrEmpty(propSchema.Title))
                    {
                        // Generate nested enum (only if not already generated)
                        typeName = className + propertyName;
                        if (generatedTypes.Add(typeName))
                        {
                            GenerateEnum(typeName, propSchema);
                        }
                    }
                    else
                    {
                        typeName = GetSchemaType(propSchema);
                    }

                    if (typeName != null)
                    {
                        var isRequired = schema.Required?.Contains(property.Key) ?? false;
                        var typeIncludesNull = config.GeneratePolymorphicModels
                            ? AllowsNull(propSchema)
                            : propSchema.Type?.HasFlag(JsonSchemaType.Null) ?? false;
                        var isValueType = IsValueType(typeName);
                        
                        // Value types are only nullable if schema explicitly includes null
                        // Reference types are nullable if not required OR schema includes null
                        var isNullable = isValueType 
                            ? typeIncludesNull 
                            : (!isRequired || typeIncludesNull);
                        var nullableSuffix = isNullable ? "?" : String.Empty;
                        
                        sb.AppendLine($"    [global::System.Text.Json.Serialization.JsonPropertyName(\"{property.Key}\")]");
                        sb.AppendLine($"    public {typeName}{nullableSuffix} {propertyName} {{ get; set; }}");
                        sb.AppendLine();
                    }
                }
            }
        }

        sb.AppendLine("}");

        AddModelSource(className, sb.ToString());

        // Generate JSON converter if requested
        if (config.GenerateJsonConverters)
        {
            if (config.GeneratePolymorphicModels)
            {
                pendingConverters.Add((className, schema));
                return;
            }
            try
            {
                // Parse the generated source code and add it to compilation so we can get the type symbol
                var parseOptions = Compilation.SyntaxTrees.FirstOrDefault()?.Options as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions ?? Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default;
                var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                    sb.ToString(), 
                    parseOptions,
                    cancellationToken: context.CancellationToken
                );
                UpdateCompilation(Compilation.AddSyntaxTrees(syntaxTree));
                
                var fullyQualifiedTypeName = $"{config.Namespace}.{className}";
                var typeSymbol = Compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
                
                if (typeSymbol != null)
                {
                    JsonConverterSourceGenerator.GenerateJsonConverter(context, typeSymbol);
                    this.convertedTypes.Add(fullyQualifiedTypeName);
                }
            }
            catch
            {
                // TODO: I need to log this out properly
                // If we can't generate the JSON converter, just skip it silently
                // This might happen for complex types or if there are other issues
            }
        }
    }

    void AddModelSource(string name, string source)
    {
        context.AddSource(GetFileName(name), SourceText.From(source, Encoding.UTF8));
        if (!config.GeneratePolymorphicModels)
            return;

        var parseOptions = Compilation.SyntaxTrees.FirstOrDefault()?.Options as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions
            ?? Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default;
        UpdateCompilation(Compilation.AddSyntaxTrees(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            source, parseOptions, cancellationToken: context.CancellationToken)));
    }

    void GenerateConverter(string className, IOpenApiSchema schema)
    {
        var fullyQualifiedTypeName = $"{config.Namespace}.{className}";
        if (polymorphism.Bases.TryGetValue(className, out var parent))
        {
            OpenApiPolymorphicConverterGenerator.Generate(context, config.Namespace, parent, config.UseInternalClasses);
        }
        else
        {
            var symbol = Compilation.GetTypeByMetadataName(fullyQualifiedTypeName)
                ?? throw new OpenApiPolymorphismException($"Cannot resolve generated model '{fullyQualifiedTypeName}' to emit its converter.");
            var options = new JsonConverterGenerationOptions { UseInternalConverter = config.UseInternalClasses };
            AddOptionalProperties(options, schema);
            if (polymorphism.Variants.TryGetValue(className, out var variant))
            {
                options.IncludeInheritedProperties = true;
                options.DiscriminatorPropertyName = variant.Parent.Discriminator;
                options.DiscriminatorValue = variant.DiscriminatorValue;
                AddRequiredProperties(options, schema, onlyPolymorphic: false);
                AddRequiredProperties(options, variant.Parent.Schema, onlyPolymorphic: false);
                AddOptionalProperties(options, variant.Parent.Schema);
            }
            else
            {
                AddRequiredProperties(options, schema, onlyPolymorphic: true);
            }
            JsonConverterSourceGenerator.GenerateJsonConverter(context, symbol, attachAttribute: true, options);
        }
        convertedTypes.Add(fullyQualifiedTypeName);
    }

    static void AddOptionalProperties(JsonConverterGenerationOptions options, IOpenApiSchema schema)
    {
        if (schema.Properties == null) return;
        foreach (var property in schema.Properties)
            if (property.Value is not null && schema.Required?.Contains(property.Key) != true && !AllowsNull(property.Value))
                options.OmitNullProperties.Add(property.Key.Pascalize().ToSafeIdentifier());
    }

    void AddRequiredProperties(JsonConverterGenerationOptions options, IOpenApiSchema schema, bool onlyPolymorphic)
    {
        if (schema.Required == null || schema.Properties == null)
            return;
        foreach (var name in schema.Required)
        {
            if (name == options.DiscriminatorPropertyName || !schema.Properties.TryGetValue(name, out var property))
                continue;
            if (onlyPolymorphic && !ReferencesPolymorphicBase(property))
                continue;

            var propertyName = name.Pascalize().ToSafeIdentifier();
            options.RequiredProperties.Add(propertyName);
            if (!AllowsNull(property))
                options.NonNullProperties.Add(propertyName);
        }
    }

    bool ReferencesPolymorphicBase(IOpenApiSchema schema)
        => schema is OpenApiSchemaReference reference
            ? polymorphism.Bases.ContainsKey(OpenApiPolymorphism.ClassName(reference.Reference.Id!))
            : schema.OneOf?.Any(ReferencesPolymorphicBase) == true || schema.AnyOf?.Any(ReferencesPolymorphicBase) == true;

    static bool AllowsNull(IOpenApiSchema schema)
        => schema.Type?.HasFlag(JsonSchemaType.Null) == true ||
           schema.OneOf?.Any(x => x.Type == JsonSchemaType.Null) == true ||
           schema.AnyOf?.Any(x => x.Type == JsonSchemaType.Null) == true;

    
    string? GetSchemaType(IOpenApiSchema schema)
    {
        if (config.GeneratePolymorphicModels)
            OpenApiPolymorphism.RejectInlineDiscriminator(schema);
        string? type = null;
        if (schema is OpenApiSchemaReference schemaRef && !String.IsNullOrWhiteSpace(schemaRef.Reference.Id))
        {
            var typeName = schemaRef.Reference.Id!.Split('/').Last().Pascalize().ToSafeIdentifier();
            // Optionally resolve and generate the referenced schema if needed
            // var resolvedSchema = schemaRef.ResolveReference(/* document context */);
            // if (resolvedSchema != null)
            //     GenerateModel(typeName, resolvedSchema);
            
            type = $"global::{config.Namespace}.{typeName}";
        }
        // Prioritize format over type - when format indicates a numeric type, use it
        // This handles cases like "type": ["integer", "string"] with "format": "int32"
        // Check this BEFORE schema.Type check since Type might be null for multi-type schemas
        else if (IsNumericFormat(schema.Format))
        {
            type = GetNumberType(schema.Format);
        }
        else if (schema.Type != null)
        {
            if (schema.Type.Value.HasFlag(JsonSchemaType.Integer))
            {
                type = GetNumberType(schema.Format);
            }
            else if (schema.Type.Value.HasFlag(JsonSchemaType.Number))
            {
                type = GetNumberType(schema.Format);
            }
            else if (schema.Type.Value.HasFlag(JsonSchemaType.Boolean))
            {
                type = "bool";
            }
            else if (schema.Type.Value.HasFlag(JsonSchemaType.Array) && schema.Items != null)
            {
                var listType = GetSchemaType(schema.Items);
                if (listType != null)
                {
                    if (config.GeneratePolymorphicModels)
                        collectionElementTypes.Add(listType);
                    type = $"global::System.Collections.Generic.List<{listType}>";
                }
            }
            else if (schema.Type.Value.HasFlag(JsonSchemaType.String))
            {
                type = GetStringType(schema);
            }
            else if (schema.Type.Value.HasFlag(JsonSchemaType.Object))
            {
                if (schema.AdditionalProperties == null)
                {
                    // Object with no additional properties and no title - might be inline definition
                    type = "object";
                }
                else
                {
                    var dictionaryValueType = GetSchemaType(schema.AdditionalProperties);
                    if (dictionaryValueType != null)
                        type = $"global::System.Collections.Generic.Dictionary<string, {dictionaryValueType}>";
                }
            }
            else
            {
                type = "object";
            }
        }
        else if (schema.AllOf?.Count == 1)
        {
            type = GetSchemaType(schema.AllOf[0]);
        }
        else if (schema.OneOf is { Count: > 0 })
        {
            // Filter out null types and get the first non-null schema
            var nonNullSchema = schema.OneOf
                .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                .FirstOrDefault();
            if (nonNullSchema != null)
                type = GetSchemaType(nonNullSchema);
        }
        else if (schema.AnyOf is { Count: > 0 })
        {
            // Filter out null types and get the first non-null schema
            var nonNullSchema = schema.AnyOf
                .Where(s => s is OpenApiSchemaReference || (s.Type?.HasFlag(JsonSchemaType.Null) != true))
                .FirstOrDefault();
            if (nonNullSchema != null)
                type = GetSchemaType(nonNullSchema);
        }

        return type;
    }
    
    static bool IsNumericFormat(string? format)
        => format is "int32" or "int64" or "float" or "double" or "decimal";

    static string GetStringType(IOpenApiSchema schema)
    {
        // Handle TimeSpan patterns (with or without optional negative sign)
        if (schema.Pattern?.Equals("^-(\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(\\.\\d{1,7})?$") ?? false)
            return "global::System.TimeSpan";
        
        if (schema.Pattern?.Equals("^-?(\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(\\.\\d{1,7})?$") ?? false)
            return "global::System.TimeSpan";

        return schema.Format switch
        {
            "date-time" => "global::System.DateTimeOffset",
            "uuid" => "global::System.Guid",
            "date" => "global::System.DateOnly",
            "time" => "global::System.TimeOnly",
            "date-span" => "global::System.TimeSpan",
            "binary" or "byte" => "byte[]",
            _ => "string"
        };
    }

    static string GetNumberType(string? format) => format switch
    {
        "int32" => "int",
        "int64" => "long",
        "float" => "float",
        "decimal" => "decimal",
        _ => "double"
    };

    string GetFileName(string typeName)
    {
        // Sanitize namespace to be file-system safe
        var sanitizedNamespace = config.Namespace.Replace("<", "_").Replace(">", "_");
        return $"{sanitizedNamespace}.{typeName}.g.cs";
    }

    static bool IsValueType(string typeName)
        => typeName is "bool" or "int" or "long" or "float" or "double" or "decimal"
            || typeName.StartsWith("global::System.Guid")
            || typeName.StartsWith("global::System.DateTime")
            || typeName.StartsWith("global::System.DateTimeOffset")
            || typeName.StartsWith("global::System.DateOnly")
            || typeName.StartsWith("global::System.TimeOnly")
            || typeName.StartsWith("global::System.TimeSpan");
}
