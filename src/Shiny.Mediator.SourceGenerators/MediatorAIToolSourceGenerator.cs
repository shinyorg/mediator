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
                return "string";

            case "bool":
                return "boolean";

            case "int":
            case "long":
            case "short":
            case "byte":
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
            "long" => "GetInt64()",
            "short" => "GetInt16()",
            "byte" => "GetByte()",
            "float" => "GetSingle()",
            "double" => "GetDouble()",
            "decimal" => "GetDecimal()",
            _ => null
        };

        if (accessor is not null)
        {
            // Non-nullable string needs ! since GetString returns string?
            if (display == "string" && !isNullable)
                return accessor + "!";
            return accessor;
        }

        // Special string-parsed types
        if (display is "System.Guid" or "System.DateTime" or "System.DateTimeOffset" or "System.DateOnly" or "System.Uri")
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

        if (value is int or long or short or byte)
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
        if (value is long l)
            return l.ToString() + "L";
        if (value is short sh)
            return "(short)" + sh.ToString();
        if (value is byte by)
            return "(byte)" + by.ToString();

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
        sb.AppendLine("        var json = global::System.Text.Json.JsonSerializer.SerializeToElement(arguments);");
        sb.AppendLine();

        // Construct the contract
        if (contract.Properties.Count > 0)
        {
            sb.AppendLine($"        var contract = new {contract.ContractType}(");
            for (var i = 0; i < contract.Properties.Count; i++)
            {
                var prop = contract.Properties[i];
                var comma = i < contract.Properties.Count - 1 ? "," : "";

                if (prop.HasDefault || prop.IsNullable)
                {
                    var varName = $"_{prop.CamelName}";
                    var accessor = GeneratePropertyAccessor(prop, varName);
                    var fallback = prop.HasDefault ? prop.DefaultCSharpLiteral : "null";
                    sb.AppendLine($"            {prop.Name}: json.TryGetProperty(\"{prop.CamelName}\", out var {varName}) && {varName}.ValueKind != global::System.Text.Json.JsonValueKind.Null ? {accessor} : {fallback}{comma}");
                }
                else
                {
                    var accessor = GeneratePropertyAccessor(prop, $"json.GetProperty(\"{prop.CamelName}\")");
                    sb.AppendLine($"            {prop.Name}: {accessor}{comma}");
                }
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

    static string GeneratePropertyAccessor(AIToolPropertyInfo prop, string elementExpr)
    {
        if (prop.JsonElementAccessor is not null)
            return $"{elementExpr}.{prop.JsonElementAccessor}";

        var display = prop.UnderlyingTypeFullName;

        // Guid
        if (display == "global::System.Guid")
            return $"global::System.Guid.Parse({elementExpr}.GetString()!)";

        // DateTime
        if (display == "global::System.DateTime")
            return $"global::System.DateTime.Parse({elementExpr}.GetString()!)";

        // DateTimeOffset
        if (display == "global::System.DateTimeOffset")
            return $"global::System.DateTimeOffset.Parse({elementExpr}.GetString()!)";

        // DateOnly
        if (display == "global::System.DateOnly")
            return $"global::System.DateOnly.Parse({elementExpr}.GetString()!)";

        // Uri
        if (display == "global::System.Uri")
            return $"new global::System.Uri({elementExpr}.GetString()!)";

        // Enum
        if (prop.IsEnum)
            return $"global::System.Enum.Parse<{prop.UnderlyingTypeFullName}>({elementExpr}.GetString()!)";

        // Fallback for complex types - use JsonElement deserialization
        return $"{elementExpr}.Deserialize<{prop.UnderlyingTypeFullName}>()";
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
