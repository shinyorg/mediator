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

    public void UpdateCompilation(Compilation newCompilation) => Compilation = newCompilation;


    IDictionary<string, IOpenApiSchema> components = new Dictionary<string, IOpenApiSchema>();
    OpenApiPolymorphism polymorphism = new();

    /// <summary>
    /// Registers the document's component schemas so <c>allOf</c> parents can be generated ahead of
    /// their children and discriminated hierarchies can be resolved.
    /// </summary>
    public void SetComponents(IDictionary<string, IOpenApiSchema> schemas, Action<string> warn)
    {
        this.components = schemas;
        this.polymorphism = OpenApiPolymorphism.Analyze(schemas, warn);
    }


    public void GenerateModel(string name, IOpenApiSchema schema, bool isComponent = false)
    {
        var className = name.Pascalize().ToSafeIdentifier();

        if (this.generatedTypes.Add(className))
        {
            var componentName = isComponent ? name : null;
            if (IsGeneratableEnum(schema))
            {
                GenerateEnum(className, schema);
            }
            else if (
                schema.Type?.HasFlag(JsonSchemaType.Object) == true || 
                schema.Properties?.Count > 0 ||
                (componentName != null && (this.polymorphism.Bases.ContainsKey(componentName) || this.polymorphism.Variants.ContainsKey(componentName)))
            )
            {
                GenerateClass(className, schema, componentName);
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
    static bool TryGetEnumInt(System.Text.Json.Nodes.JsonNode node, out int value)
    {
        value = 0;
        if (node.GetValueKind() != System.Text.Json.JsonValueKind.Number)
            return false;

        if (!Decimal.TryParse(node.ToJsonString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return false;

        if (d != Decimal.Truncate(d) || d < Int32.MinValue || d > Int32.MaxValue)
            return false;

        value = (int)d;
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
        }

        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"{accessor} enum {enumName}");
        sb.AppendLine("{");

        if (isStringEnum)
        {
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
            foreach (var ev in schema.Enum!)
            {
                if (ev != null)
                {
                    TryGetEnumInt(ev, out var intValue);
                    var memberName = intValue < 0 ? $"ValueMinus{-(long)intValue}" : $"Value{intValue}";
                    sb.AppendLine($"    {memberName} = {intValue},");
                }
            }
        }

        sb.AppendLine("}");

        context.AddSource(GetFileName(enumName), SourceText.From(sb.ToString(), Encoding.UTF8));
    }
    

    void GenerateClass(string className, IOpenApiSchema schema, string? componentName = null)
    {
        OpenApiPolymorphicBase? polymorphicBase = null;
        OpenApiPolymorphicVariant? polymorphicVariant = null;
        if (componentName != null)
        {
            this.polymorphism.Bases.TryGetValue(componentName, out polymorphicBase);
            this.polymorphism.Variants.TryGetValue(componentName, out polymorphicVariant);
        }

        // allOf: [$ref Parent, {...}] - or the discriminated base for union style variants
        var parentComponent = polymorphicVariant?.ParentComponent;
        string? parentType = null;
        if (parentComponent == null && componentName != null)
            parentComponent = OpenApiPolymorphism.GetParent(componentName, this.components);

        if (parentComponent != null && this.components.TryGetValue(parentComponent, out var parentSchema))
        {
            // the parent must exist (and be in the compilation for json converter generation) first
            this.GenerateModel(parentComponent, parentSchema, true);
            parentType = $"global::{config.Namespace}.{parentComponent.Pascalize().ToSafeIdentifier()}";
        }
        else if (schema.AllOf?.Count == 1)
        {
            parentType = GetSchemaType(schema.AllOf[0]);
        }

        var discriminator = (polymorphicBase ?? polymorphicVariant?.Root)?.PropertyName;
        var inherited = parentComponent == null 
            ? new HashSet<string>() 
            : GetInheritedPropertyNames(parentComponent);
        
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        if (polymorphicBase != null)
            sb.AppendLine($"[global::System.Text.Json.Serialization.JsonConverter(typeof(global::{config.Namespace}.{className}JsonConverter))]");
        
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.Append($"{accessor} {(polymorphicBase == null ? "" : "abstract ")}partial class {className}");
        sb.AppendLine(parentType == null ? "" : $" : {parentType}");
        sb.AppendLine("{");

        if (discriminator != null)
        {
            // the discriminator is always written first - receivers like System.Text.Json require it to lead the object
            // "$type" (ASP.NET Core's default) → Type
            var discriminatorName = discriminator.TrimStart('$', '@').Pascalize().ToSafeIdentifier();
            sb.AppendLine($"    [global::System.Text.Json.Serialization.JsonPropertyName(\"{Escape(discriminator)}\")]");
            sb.AppendLine("    [global::System.Text.Json.Serialization.JsonPropertyOrder(-1)]");
            if (polymorphicBase != null)
                sb.AppendLine($"    public abstract string {discriminatorName} {{ get; set; }}");
            else
                sb.AppendLine($"    public override string {discriminatorName} {{ get; set; }} = \"{Escape(polymorphicVariant!.Tag)}\";");
            sb.AppendLine();
        }

        // Generate properties
        var required = new HashSet<string>(GetSchemaParts(schema).SelectMany(x => x.Required ?? Enumerable.Empty<string>()));
        foreach (var property in GetProperties(schema))
        {
            // inherited properties are declared on the parent - redeclaring would hide them
            if (property.Key == discriminator || inherited.Contains(property.Key))
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
                    var isRequired = required.Contains(property.Key);
                    var typeIncludesNull = propSchema.Type?.HasFlag(JsonSchemaType.Null) ?? false;
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

        sb.AppendLine("}");

        var source = sb.ToString();
        context.AddSource(GetFileName(className), SourceText.From(source, Encoding.UTF8));

        // abstract bases dispatch on the discriminator in both reflection and generated converter modes
        if (polymorphicBase != null)
            GeneratePolymorphicConverter(className, polymorphicBase);

        // Generate JSON converter if requested
        if (config.GenerateJsonConverters)
        {
            try
            {
                // Parse the generated source code and add it to compilation so we can get the type symbol
                var parseOptions = Compilation.SyntaxTrees.FirstOrDefault()?.Options as Microsoft.CodeAnalysis.CSharp.CSharpParseOptions ?? Microsoft.CodeAnalysis.CSharp.CSharpParseOptions.Default;
                var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                    source, 
                    parseOptions,
                    cancellationToken: context.CancellationToken
                );
                UpdateCompilation(Compilation.AddSyntaxTrees(syntaxTree));
                
                var fullyQualifiedTypeName = $"{config.Namespace}.{className}";
                if (polymorphicBase != null)
                {
                    // the dispatching converter above stands in for the per-type converter
                    this.convertedTypes.Add(fullyQualifiedTypeName);
                }
                else
                {
                    var typeSymbol = Compilation.GetTypeByMetadataName(fullyQualifiedTypeName);
                    if (typeSymbol != null)
                    {
                        JsonConverterSourceGenerator.GenerateJsonConverter(context, typeSymbol);
                        this.convertedTypes.Add(fullyQualifiedTypeName);
                    }
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


    void GeneratePolymorphicConverter(string className, OpenApiPolymorphicBase info)
    {
        var typeRef = $"global::{config.Namespace}.{className}";
        var tagName = Escape(info.PropertyName);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator OpenAPI Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable disable");
        sb.AppendLine();
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine();
        sb.AppendLine($"namespace {config.Namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Reads the \"{tagName}\" discriminator (anywhere in the object) and hands the payload to the matching");
        sb.AppendLine("/// variant's own JsonTypeInfo - reflection or generated, whichever the options resolve.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"{accessor} sealed class {className}JsonConverter : global::System.Text.Json.Serialization.JsonConverter<{typeRef}>");
        sb.AppendLine("{");
        sb.AppendLine($"    public override {typeRef} Read(ref Utf8JsonReader reader, global::System.Type typeToConvert, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (reader.TokenType == JsonTokenType.Null)");
        sb.AppendLine("            return null;");
        sb.AppendLine();
        sb.AppendLine("        if (reader.TokenType != JsonTokenType.StartObject)");
        sb.AppendLine("            throw new JsonException(\"Expected StartObject token\");");
        sb.AppendLine();
        sb.AppendLine("        var discriminator = ReadDiscriminator(reader);");
        sb.AppendLine("        switch (discriminator)");
        sb.AppendLine("        {");
        foreach (var variant in info.Variants)
        {
            sb.AppendLine($"            case \"{Escape(variant.Key)}\":");
            sb.AppendLine($"                return Deserialize<{VariantType(variant.Value)}>(ref reader, options);");
        }
        sb.AppendLine("            case null:");
        sb.AppendLine($"                throw new JsonException(\"Missing '{tagName}' discriminator for {className}\");");
        sb.AppendLine("            default:");
        sb.AppendLine($"                throw new JsonException($\"Unknown '{tagName}' discriminator '{{discriminator}}' for {className}\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine($"    public override void Write(Utf8JsonWriter writer, {typeRef} value, JsonSerializerOptions options)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            writer.WriteNullValue();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // exact type match - an intermediate variant must not swallow its subclasses");
        sb.AppendLine("        var type = value.GetType();");
        foreach (var variant in info.Variants)
        {
            var variantType = VariantType(variant.Value);
            sb.AppendLine($"        if (type == typeof({variantType}))");
            sb.AppendLine("        {");
            sb.AppendLine($"            Serialize(writer, ({variantType})value, options);");
            sb.AppendLine("            return;");
            sb.AppendLine("        }");
        }
        sb.AppendLine($"        throw new JsonException($\"'{{type}}' is not a known '{tagName}' variant of {className}\");");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    static T Deserialize<T>(ref Utf8JsonReader reader, JsonSerializerOptions options)");
        sb.AppendLine("        => JsonSerializer.Deserialize(ref reader, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));");
        sb.AppendLine();
        sb.AppendLine("    static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options)");
        sb.AppendLine("        => JsonSerializer.Serialize(writer, value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));");
        sb.AppendLine();
        sb.AppendLine("    // reader is a copy - scanning ahead leaves the caller's position untouched");
        sb.AppendLine("    static string ReadDiscriminator(Utf8JsonReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine("        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var isDiscriminator = reader.ValueTextEquals(\"{tagName}\");");
        sb.AppendLine("            reader.Read();");
        sb.AppendLine("            if (isDiscriminator)");
        sb.AppendLine("                return reader.TokenType == JsonTokenType.String ? reader.GetString() : null;");
        sb.AppendLine();
        sb.AppendLine("            reader.Skip();");
        sb.AppendLine("        }");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(GetFileName(className + "JsonConverter"), SourceText.From(sb.ToString(), Encoding.UTF8));
    }


    string VariantType(string componentName) 
        => $"global::{config.Namespace}.{componentName.Pascalize().ToSafeIdentifier()}";


    /// <summary>The schema itself plus its inline (non-$ref) allOf members, which all contribute properties.</summary>
    static IEnumerable<IOpenApiSchema> GetSchemaParts(IOpenApiSchema schema)
    {
        yield return schema;
        if (schema.AllOf != null)
        {
            foreach (var part in schema.AllOf)
            {
                if (part is not OpenApiSchemaReference)
                    yield return part;
            }
        }
    }


    static IEnumerable<KeyValuePair<string, IOpenApiSchema>> GetProperties(IOpenApiSchema schema)
    {
        var seen = new HashSet<string>();
        foreach (var part in GetSchemaParts(schema))
        {
            if (part.Properties == null)
                continue;

            foreach (var property in part.Properties)
            {
                if (seen.Add(property.Key))
                    yield return property;
            }
        }
    }


    HashSet<string> GetInheritedPropertyNames(string componentName)
    {
        var names = new HashSet<string>();
        var visited = new HashSet<string>();
        string? current = componentName;
        while (current != null && visited.Add(current) && this.components.TryGetValue(current, out var schema))
        {
            foreach (var property in GetProperties(schema))
                names.Add(property.Key);

            current = this.polymorphism.Variants.TryGetValue(current, out var variant)
                ? variant.ParentComponent
                : OpenApiPolymorphism.GetParent(current, this.components);
        }
        return names;
    }


    static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");


    string? GetSchemaType(IOpenApiSchema schema)
    {
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
                    type = $"global::System.Collections.Generic.List<{listType}>";
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
        => format is "int32" or "int64" or "float" or "double";

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
            _ => "string"
        };
    }

    static string GetNumberType(string? format) => format switch
    {
        "int32" => "int",
        "int64" => "long",
        "float" => "float",
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