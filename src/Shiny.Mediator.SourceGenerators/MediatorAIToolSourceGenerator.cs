using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class MediatorAIToolSourceGenerator : IIncrementalGenerator
{
    static readonly DiagnosticDescriptor MissingAIPackageError = new(
        "SHINYMED100",
        "Microsoft.Extensions.AI is required",
        "ShinyMediatorGenerateAITools is enabled but Microsoft.Extensions.AI is not referenced. Add a PackageReference to Microsoft.Extensions.AI.",
        "ShinyMediator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                               SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Read the MSBuild property to check if AI tools generation is enabled
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.ShinyMediatorGenerateAITools", out var value);
                return string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
            });

        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null);

        var compilationAndClasses = context
            .CompilationProvider
            .Combine(classDeclarations.Collect())
            .Combine(enabledProvider);

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) =>
            {
                var enabled = source.Right;
                if (!enabled)
                    return;

                var compilation = source.Left.Left;
                var contracts = source.Left.Right;

                // Check if Microsoft.Extensions.AI is referenced
                var hasAIPackage = compilation.ReferencedAssemblyNames
                    .Any(a => a.Name == "Microsoft.Extensions.AI.Abstractions");

                if (!hasAIPackage)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(MissingAIPackageError, Location.None));
                    return;
                }

                Execute(compilation, contracts!, spc);
            }
        );
    }

    static AIToolContractInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;
        if (typeSymbol is null || typeSymbol.IsAbstract)
            return null;

        // Read [Description] attribute value
        var descAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");

        if (descAttr is null)
            return null;

        var description = descAttr.ConstructorArguments.Length > 0
            ? descAttr.ConstructorArguments[0].Value?.ToString() ?? ""
            : "";

        // Check if it implements IRequest<T> or ICommand
        string? resultType = null;
        var isCommand = false;

        foreach (var intf in typeSymbol.AllInterfaces)
        {
            if (!intf.IsGenericType)
            {
                if (intf.ToDisplayString() == "Shiny.Mediator.ICommand")
                {
                    isCommand = true;
                    break;
                }
                continue;
            }

            if (intf.OriginalDefinition.ToDisplayString() == "Shiny.Mediator.IRequest<TResult>")
            {
                resultType = intf.TypeArguments[0].ToDisplayString(FullyQualifiedFormat);
                break;
            }
        }

        if (!isCommand && resultType is null)
            return null;

        // Gather property info
        var properties = GetPropertyInfos(typeSymbol);

        var contractNamespace = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return new AIToolContractInfo(
            typeSymbol.ToDisplayString(FullyQualifiedFormat),
            typeSymbol.Name,
            contractNamespace,
            description,
            resultType,
            isCommand,
            properties
        );
    }

    static List<AIToolPropertyInfo> GetPropertyInfos(INamedTypeSymbol typeSymbol)
    {
        var properties = new List<AIToolPropertyInfo>();

        // Build a map of constructor parameter defaults
        var ctorDefaults = new Dictionary<string, object?>();
        var ctor = typeSymbol.Constructors
            .Where(c => !c.IsImplicitlyDeclared || typeSymbol.IsRecord)
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (ctor is not null)
        {
            foreach (var param in ctor.Parameters)
            {
                if (param.HasExplicitDefaultValue)
                    ctorDefaults[param.Name] = param.ExplicitDefaultValue;
            }
        }

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;

            if (prop.IsStatic || prop.DeclaredAccessibility != Accessibility.Public)
                continue;

            // For records, properties from primary ctor are init-only but valid
            // For classes, we need read+write
            if (!typeSymbol.IsRecord && (prop.IsReadOnly || prop.SetMethod is null))
                continue;

            var propType = prop.Type;
            var isNullable = IsNullableType(prop);
            var underlyingType = GetUnderlyingType(propType);

            var jsonSchemaType = GetJsonSchemaType(underlyingType);
            var jsonElementAccessor = GetJsonElementAccessor(underlyingType, isNullable);

            // Read [Description] attribute on property
            var propDescAttr = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.ComponentModel.DescriptionAttribute");
            var propDescription = propDescAttr?.ConstructorArguments.Length > 0
                ? propDescAttr.ConstructorArguments[0].Value?.ToString()
                : null;

            // Check for default value
            var hasDefault = ctorDefaults.TryGetValue(prop.Name, out var defaultVal);

            string? defaultJsonLiteral = null;
            string? defaultCSharpLiteral = null;
            if (hasDefault)
            {
                defaultJsonLiteral = ToJsonLiteral(defaultVal, underlyingType);
                defaultCSharpLiteral = ToCSharpLiteral(defaultVal, underlyingType);
            }

            // Enum values
            var isEnum = underlyingType.TypeKind == TypeKind.Enum;
            List<string> enumValues = null;
            if (isEnum)
            {
                enumValues = underlyingType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.HasConstantValue)
                    .Select(f => f.Name)
                    .ToList();
            }

            var camelName = char.ToLowerInvariant(prop.Name[0]) + prop.Name.Substring(1);

            properties.Add(new AIToolPropertyInfo(
                prop.Name,
                camelName,
                jsonSchemaType,
                propDescription,
                isNullable,
                hasDefault,
                defaultJsonLiteral,
                defaultCSharpLiteral,
                isEnum,
                enumValues,
                jsonElementAccessor,
                underlyingType.ToDisplayString(FullyQualifiedFormat)
            ));
        }

        return properties;
    }

    static bool IsNullableType(IPropertySymbol prop)
    {
        // Nullable<T> value type
        if (prop.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return true;

        // NRT annotated reference type
        return prop.NullableAnnotation == NullableAnnotation.Annotated;
    }

    static ITypeSymbol GetUnderlyingType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            return named.TypeArguments[0];
        return type;
    }

    static string GetJsonSchemaType(ITypeSymbol type)
    {
        var display = type.ToDisplayString().TrimEnd('?');

        switch (display)
        {
            case "string":
            case "System.Guid":
            case "System.Uri":
            case "System.DateTime":
            case "System.DateTimeOffset":
            case "System.DateOnly":
            case "System.TimeOnly":
            case "System.TimeSpan":
                return "string";

            case "bool":
                return "boolean";

            case "int":
            case "uint":
            case "long":
            case "ulong":
            case "short":
            case "ushort":
            case "byte":
            case "sbyte":
                return "integer";

            case "float":
            case "double":
            case "decimal":
                return "number";
        }

        if (type.TypeKind == TypeKind.Enum)
            return "string";

        if (type is IArrayTypeSymbol)
            return "array";

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var iEnumerable = namedType.AllInterfaces
                .Any(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
            if (iEnumerable)
                return "array";
        }

        return "object";
    }

    static string? GetJsonElementAccessor(ITypeSymbol type, bool isNullable)
    {
        var display = type.ToDisplayString().TrimEnd('?');

        var accessor = display switch
        {
            "string" => "GetString()",
            "bool" => "GetBoolean()",
            "int" => "GetInt32()",
            "uint" => "GetUInt32()",
            "long" => "GetInt64()",
            "ulong" => "GetUInt64()",
            "short" => "GetInt16()",
            "ushort" => "GetUInt16()",
            "byte" => "GetByte()",
            "sbyte" => "GetSByte()",
            "float" => "GetSingle()",
            "double" => "GetDouble()",
            "decimal" => "GetDecimal()",
            "System.Guid" => "GetGuid()",
            "System.DateTime" => "GetDateTime()",
            "System.DateTimeOffset" => "GetDateTimeOffset()",
            _ => null
        };

        if (accessor is not null)
        {
            // Non-nullable string needs ! since GetString returns string?
            if (display == "string" && !isNullable)
                return accessor + "!";
            return accessor;
        }

        // String-parsed types
        if (display is "System.DateOnly" or "System.TimeOnly" or "System.TimeSpan" or "System.Uri")
            return null; // handled specially in code gen

        if (type.TypeKind == TypeKind.Enum)
            return null; // handled specially in code gen

        return null; // complex types handled specially
    }

    static string ToJsonLiteral(object value, ITypeSymbol type)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return "\"" + EscapeJsonString(s) + "\"";

        if (value is bool b)
            return b ? "true" : "false";

        if (value is int or uint or long or ulong or short or ushort or byte or sbyte)
            return value.ToString();

        if (value is float f)
            return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is double d)
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (value is decimal m)
            return m.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return "\"" + EscapeJsonString(value.ToString()) + "\"";
    }

    static string ToCSharpLiteral(object value, ITypeSymbol type)
    {
        if (value is null)
            return "null";

        if (value is string s)
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        if (value is bool b)
            return b ? "true" : "false";

        if (value is int i)
            return i.ToString();
        if (value is uint ui)
            return ui.ToString() + "U";
        if (value is long l)
            return l.ToString() + "L";
        if (value is ulong ul)
            return ul.ToString() + "UL";
        if (value is short sh)
            return "(short)" + sh.ToString();
        if (value is ushort us)
            return "(ushort)" + us.ToString();
        if (value is byte by)
            return "(byte)" + by.ToString();
        if (value is sbyte sb2)
            return "(sbyte)" + sb2.ToString();

        if (value is float f)
            return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
        if (value is double d)
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "d";
        if (value is decimal m)
            return m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";

        // Enum default values come as their underlying integral type
        if (type.TypeKind == TypeKind.Enum)
        {
            var enumType = type.ToDisplayString(FullyQualifiedFormat);
            // Try to find the matching enum member
            foreach (var member in type.GetMembers().OfType<IFieldSymbol>())
            {
                if (member.HasConstantValue && member.ConstantValue?.Equals(value) == true)
                    return enumType + "." + member.Name;
            }
            return "(" + enumType + ")" + value;
        }

        return value.ToString();
    }

    static string EscapeJsonString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    static void Execute(Compilation compilation, ImmutableArray<AIToolContractInfo?> contracts, SourceProductionContext context)
    {
        var validContracts = contracts
            .Where(c => c is not null)
            .Cast<AIToolContractInfo>()
            .Distinct()
            .ToList();

        if (!validContracts.Any())
            return;

        var nameSpace = compilation.AssemblyName ?? "Generated";

        // Generate per-contract AIFunction classes
        foreach (var contract in validContracts)
        {
            var classSource = GenerateAIFunctionClass(contract, nameSpace);
            context.AddSource(
                $"{contract.ContractTypeName}AIFunction.g.cs",
                SourceText.From(classSource, Encoding.UTF8)
            );
        }

        // Generate registration method
        var registrationSource = GenerateRegistration(validContracts, nameSpace);
        context.AddSource("MediatorAIToolRegistration.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
    }

    static string GenerateAIFunctionClass(AIToolContractInfo contract, string nameSpace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {nameSpace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine($"internal sealed class {contract.ContractTypeName}AIFunction : global::Microsoft.Extensions.AI.AIFunction");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly global::Shiny.Mediator.IMediator _mediator;");
        sb.AppendLine();

        // Static schema
        var schemaJson = BuildSchemaJson(contract);
        sb.AppendLine($"    private static readonly global::System.Text.Json.JsonElement _jsonSchema =");
        sb.AppendLine($"        global::System.Text.Json.JsonDocument.Parse(\"{EscapeCSharpString(schemaJson)}\").RootElement.Clone();");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"    public {contract.ContractTypeName}AIFunction(global::Shiny.Mediator.IMediator mediator)");
        sb.AppendLine("    {");
        sb.AppendLine("        _mediator = mediator;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Properties
        sb.AppendLine($"    public override string Name => \"{contract.ContractTypeName}\";");
        sb.AppendLine($"    public override string Description => \"{EscapeCSharpString(contract.Description)}\";");
        sb.AppendLine("    public override global::System.Text.Json.JsonElement JsonSchema => _jsonSchema;");
        sb.AppendLine();

        // InvokeCoreAsync
        sb.AppendLine("    protected override async global::System.Threading.Tasks.ValueTask<object?> InvokeCoreAsync(");
        sb.AppendLine("        global::Microsoft.Extensions.AI.AIFunctionArguments arguments,");
        sb.AppendLine("        global::System.Threading.CancellationToken cancellationToken)");
        sb.AppendLine("    {");
        // Read each contract property directly from the AIFunctionArguments dictionary so we never
        // do `JsonSerializer.SerializeToElement(arguments)` — that path is reflection-based and
        // throws under AOT. Values in the dictionary may be already-typed primitives (string, long,
        // double, bool) or a JsonElement when the LLM/runtime hands us a structured payload;
        // EmitArgumentReadBlock handles both shapes.
        if (contract.Properties.Count > 0)
        {
            foreach (var prop in contract.Properties)
                EmitArgumentReadBlock(sb, prop);

            sb.AppendLine($"        var contract = new {contract.ContractType}(");
            for (var i = 0; i < contract.Properties.Count; i++)
            {
                var prop = contract.Properties[i];
                var comma = i < contract.Properties.Count - 1 ? "," : "";
                sb.AppendLine($"            {prop.Name}: __val_{prop.CamelName}{comma}");
            }
            sb.AppendLine("        );");
        }
        else
        {
            sb.AppendLine($"        var contract = new {contract.ContractType}();");
        }

        sb.AppendLine();

        if (contract.IsCommand)
        {
            sb.AppendLine("        await _mediator.Send(contract, cancellationToken).ConfigureAwait(false);");
            sb.AppendLine("        return \"Command executed successfully\";");
        }
        else
        {
            sb.AppendLine($"        var (_, result) = await _mediator.Request<{contract.ResultType}>(contract, cancellationToken).ConfigureAwait(false);");
            sb.AppendLine("        return result;");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Emits the multi-line read+coerce block for a single contract property. Output declares
    /// <c>__val_{camelName}</c> typed as the property's declared type; downstream code references
    /// that variable when constructing the contract. The block handles three runtime shapes the
    /// dictionary value can take:
    /// <list type="bullet">
    /// <item>Already-typed primitive (string / int / long / double / bool / etc.)</item>
    /// <item>A <c>JsonElement</c> wrapping a structured value (the LLM/runtime path)</item>
    /// <item>null — falls back to the property default, or <c>null</c> for nullable types, or
    /// throws for required non-nullable properties</item>
    /// </list>
    /// </summary>
    static void EmitArgumentReadBlock(StringBuilder sb, AIToolPropertyInfo prop)
    {
        var raw = $"__raw_{prop.CamelName}";
        var val = $"__val_{prop.CamelName}";
        var name = prop.CamelName;
        var t = prop.UnderlyingTypeFullName;
        var typeRef = (prop.HasDefault || prop.IsNullable) && !prop.IsEnum && t != "string" && !t.StartsWith("global::")
            ? $"{t}?" : (prop.IsNullable && !IsReferenceLike(t) ? $"{t}?" : t);
        // Reference-like (string, complex, Uri) declared as nullable when IsNullable so we can
        // hold null; value types are widened to T? for nullable-from-source.
        if (prop.IsNullable && IsValueLike(t))
            typeRef = $"{t}?";
        else if (prop.IsNullable && IsReferenceLike(t))
            typeRef = $"{t}?";
        else
            typeRef = t;

        var fallback = prop.HasDefault
            ? prop.DefaultCSharpLiteral
            : prop.IsNullable
                ? "null"
                : $"throw new global::System.ArgumentException(\"Missing required argument '{name}'\")";

        sb.AppendLine($"        arguments.TryGetValue(\"{name}\", out var {raw});");
        sb.AppendLine($"        {typeRef} {val} = {raw} switch");
        sb.AppendLine("        {");
        sb.AppendLine($"            null => {fallback},");
        EmitSwitchArmsForType(sb, prop, raw, fallback);
        sb.AppendLine("        };");
        sb.AppendLine();
    }


    static bool IsReferenceLike(string typeFullName)
        => typeFullName == "string"
        || typeFullName == "global::System.Uri"
        || typeFullName.StartsWith("global::System.Text.Json.")
        || (typeFullName.StartsWith("global::") && !IsValueLike(typeFullName));


    static bool IsValueLike(string typeFullName)
        => typeFullName is "bool" or "int" or "uint" or "long" or "ulong" or "short" or "ushort"
            or "byte" or "sbyte" or "float" or "double" or "decimal"
            or "global::System.Guid"
            or "global::System.DateTime"
            or "global::System.DateTimeOffset"
            or "global::System.DateOnly"
            or "global::System.TimeOnly"
            or "global::System.TimeSpan";


    /// <summary>
    /// Per-type switch arms covering both the JsonElement path (from a structured tool call) and
    /// the already-boxed primitive path. Complex types route through Shiny.Json.Default.Options so
    /// the chain (which the mediator generator wires) provides the JsonTypeInfo.
    /// </summary>
    static void EmitSwitchArmsForType(StringBuilder sb, AIToolPropertyInfo prop, string rawVar, string fallback)
    {
        var t = prop.UnderlyingTypeFullName;
        const string JE = "global::System.Text.Json.JsonElement";

        // Enums (string-encoded). Generated schema declares enums as strings, so JsonElement comes
        // through as a string; boxed values may already be the enum.
        if (prop.IsEnum)
        {
            sb.AppendLine($"            {t} __enumVal => __enumVal,");
            sb.AppendLine($"            string __s => global::System.Enum.Parse<{t}>(__s),");
            sb.AppendLine($"            {JE} __e => global::System.Enum.Parse<{t}>(__e.GetString()!),");
            sb.AppendLine($"            _ => {fallback}");
            return;
        }

        // String-parsed value types
        switch (t)
        {
            case "global::System.DateOnly":
                sb.AppendLine($"            global::System.DateOnly __v => __v,");
                sb.AppendLine($"            string __s => global::System.DateOnly.Parse(__s),");
                sb.AppendLine($"            {JE} __e => global::System.DateOnly.Parse(__e.GetString()!),");
                sb.AppendLine($"            _ => {fallback}");
                return;

            case "global::System.TimeOnly":
                sb.AppendLine($"            global::System.TimeOnly __v => __v,");
                sb.AppendLine($"            string __s => global::System.TimeOnly.Parse(__s),");
                sb.AppendLine($"            {JE} __e => global::System.TimeOnly.Parse(__e.GetString()!),");
                sb.AppendLine($"            _ => {fallback}");
                return;

            case "global::System.TimeSpan":
                sb.AppendLine($"            global::System.TimeSpan __v => __v,");
                sb.AppendLine($"            string __s => global::System.TimeSpan.Parse(__s),");
                sb.AppendLine($"            {JE} __e => global::System.TimeSpan.Parse(__e.GetString()!),");
                sb.AppendLine($"            _ => {fallback}");
                return;

            case "global::System.Uri":
                sb.AppendLine($"            global::System.Uri __v => __v,");
                sb.AppendLine($"            string __s => new global::System.Uri(__s),");
                sb.AppendLine($"            {JE} __e => new global::System.Uri(__e.GetString()!),");
                sb.AppendLine($"            _ => {fallback}");
                return;
        }

        // Primitives — accept the already-typed boxed value OR a JsonElement. JsonElementAccessor
        // already encodes the trailing "!" for non-nullable string (see GetJsonElementAccessor).
        if (prop.JsonElementAccessor is not null)
        {
            sb.AppendLine($"            {t} __v => __v,");
            // Numeric widening — long/double can come through where int/float was declared.
            EmitNumericWidening(sb, t);
            sb.AppendLine($"            {JE} __e => __e.{prop.JsonElementAccessor},");
            sb.AppendLine($"            _ => {fallback}");
            return;
        }

        // Complex / collection types: must come as JsonElement. Use Shiny.Json's chain to deserialize
        // (the mediator contracts resolver registers JsonTypeInfo for contract types and their
        // transitively-discovered nested types).
        sb.AppendLine($"            {t} __v => __v,");
        sb.AppendLine($"            {JE} __e => global::System.Text.Json.JsonSerializer.Deserialize(__e, (global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<{t}>)global::Shiny.Json.Default.Options.GetTypeInfo(typeof({t})))!,");
        sb.AppendLine($"            _ => {fallback}");
    }


    /// <summary>
    /// Emits switch arms that widen common JSON-deserialized numeric boxings to the declared
    /// property type — e.g. when the dict holds a <c>long</c> but the property is <c>int</c>.
    /// </summary>
    static void EmitNumericWidening(StringBuilder sb, string t)
    {
        switch (t)
        {
            case "int":
                sb.AppendLine("            long __l => (int)__l,");
                break;
            case "uint":
                sb.AppendLine("            long __l => (uint)__l,");
                break;
            case "short":
                sb.AppendLine("            long __l => (short)__l,");
                break;
            case "ushort":
                sb.AppendLine("            long __l => (ushort)__l,");
                break;
            case "byte":
                sb.AppendLine("            long __l => (byte)__l,");
                break;
            case "sbyte":
                sb.AppendLine("            long __l => (sbyte)__l,");
                break;
            case "float":
                sb.AppendLine("            double __d => (float)__d,");
                break;
            case "decimal":
                sb.AppendLine("            double __d => (decimal)__d,");
                sb.AppendLine("            long __l => (decimal)__l,");
                break;
            case "long":
                sb.AppendLine("            int __i => (long)__i,");
                break;
            case "double":
                sb.AppendLine("            long __l => (double)__l,");
                break;
        }
    }

    static string BuildSchemaJson(AIToolContractInfo contract)
    {
        var sb = new StringBuilder();
        sb.Append("{\"type\":\"object\"");

        if (contract.Properties.Count > 0)
        {
            sb.Append(",\"properties\":{");
            for (var i = 0; i < contract.Properties.Count; i++)
            {
                var prop = contract.Properties[i];
                if (i > 0) sb.Append(",");

                sb.Append($"\"{prop.CamelName}\":{{");

                var schemaFields = new List<string>();

                if (prop.Description is not null)
                    schemaFields.Add($"\"description\":\"{EscapeJsonString(prop.Description)}\"");

                schemaFields.Add($"\"type\":\"{prop.JsonSchemaType}\"");

                if (prop.IsEnum && prop.EnumValues is not null && prop.EnumValues.Count > 0)
                {
                    var enumVals = string.Join(",", prop.EnumValues.Select(v => $"\"{v}\""));
                    schemaFields.Add($"\"enum\":[{enumVals}]");
                }

                if (prop.HasDefault && prop.DefaultJsonLiteral is not null && prop.DefaultJsonLiteral != "null")
                    schemaFields.Add($"\"default\":{prop.DefaultJsonLiteral}");

                sb.Append(string.Join(",", schemaFields));
                sb.Append("}");
            }
            sb.Append("}");

            // Required array
            var required = contract.Properties
                .Where(p => !p.IsNullable && !p.HasDefault)
                .ToList();

            if (required.Any())
            {
                sb.Append(",\"required\":[");
                sb.Append(string.Join(",", required.Select(p => $"\"{p.CamelName}\"")));
                sb.Append("]");
            }
        }

        sb.Append("}");
        return sb.ToString();
    }

    static string GenerateRegistration(List<AIToolContractInfo> contracts, string nameSpace)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {nameSpace};");
        sb.AppendLine();
        sb.AppendLine(Constants.GeneratedCodeAttributeString);
        sb.AppendLine("public static class MediatorAIToolRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers all mediator contracts annotated with [Description] as AI tools");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static global::Shiny.Mediator.ShinyMediatorBuilder AddGeneratedAITools(this global::Shiny.Mediator.ShinyMediatorBuilder builder)");
        sb.AppendLine("    {");

        foreach (var contract in contracts)
        {
            sb.AppendLine($"        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Microsoft.Extensions.AI.AITool>(builder.Services, sp =>");
            sb.AppendLine($"            new {contract.ContractTypeName}AIFunction(global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Shiny.Mediator.IMediator>(sp)));");
        }

        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    static string EscapeCSharpString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}

internal record AIToolContractInfo(
    string ContractType,
    string ContractTypeName,
    string? ContractNamespace,
    string Description,
    string? ResultType,
    bool IsCommand,
    List<AIToolPropertyInfo> Properties
);

internal record AIToolPropertyInfo(
    string Name,
    string CamelName,
    string JsonSchemaType,
    string? Description,
    bool IsNullable,
    bool HasDefault,
    string? DefaultJsonLiteral,
    string? DefaultCSharpLiteral,
    bool IsEnum,
    List<string>? EnumValues,
    string? JsonElementAccessor,
    string UnderlyingTypeFullName
);
