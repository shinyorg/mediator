using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shiny.Mediator.Contracts.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class ContractKeySourceGenerator : IIncrementalGenerator
{
    const string AttributeName = "Shiny.Mediator.ContractKeyAttribute";
    const string IContractKeyInterface = "Shiny.Mediator.IContractKey";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidate(s),
                transform: static (ctx, _) => GetSemanticTarget(ctx)
            )
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var classList = source.Right;
            foreach (var item in classList)
            {
                if (item is not { } tuple) continue;
                var (decl, model) = tuple;
                var symbolBase = model.GetDeclaredSymbol(decl);
                if (symbolBase is not INamedTypeSymbol symbol)
                    continue;
                var attribute = symbol.GetAttributes().FirstOrDefault(x => 
                    x.AttributeClass?.ToDisplayString() == AttributeName || 
                    x.AttributeClass?.ToDisplayString() == "Shiny.Mediator.ContractKeyAttribute" ||
                    x.AttributeClass?.Name == "ContractKeyAttribute");
                if (attribute == null)
                    continue;

                // 1. Validate partial
                Location location;
                if (!decl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    location = decl switch
                    {
                        ClassDeclarationSyntax cds => cds.Identifier.GetLocation(),
                        RecordDeclarationSyntax rds => rds.Identifier.GetLocation(),
                        _ => decl.GetLocation()
                    };
                    spc.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "SMC001",
                            title: "Class/Record must be partial",
                            messageFormat: $"Type '{symbol.Name}' must be partial to use [ContractKey]",
                            category: "Shiny.Mediator.Contracts",
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        location));
                    continue;
                }

                // 2. Parse format string and referenced properties
                var formatString = attribute.ConstructorArguments.Length > 0 
                    ? attribute.ConstructorArguments[0].Value as string 
                    : null;
                
                List<(string prop, string? format)> propertyOrder;
                
                if (string.IsNullOrWhiteSpace(formatString))
                {
                    // No format string provided - use all properties in declaration order
                    propertyOrder = symbol.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.GetMethod != null)
                        .Select(p => (p.Name, (string?)null))
                        .ToList();
                }
                else
                {
                    propertyOrder = ParseFormatString(formatString!);
                }

                if (propertyOrder.Count == 0)
                    continue;


                // 3. Validate all referenced properties exist
                var missingProps = propertyOrder.Where(p => symbol.GetMembers().OfType<IPropertySymbol>().All(x => x.Name != p.prop)).ToList();
                if (missingProps.Any())
                {
                    location = decl switch
                    {
                        ClassDeclarationSyntax cds => cds.Identifier.GetLocation(),
                        RecordDeclarationSyntax rds => rds.Identifier.GetLocation(),
                        _ => decl.GetLocation()
                    };
                    foreach (var missing in missingProps)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: "SMC002",
                                title: "Missing property",
                                messageFormat: $"Type '{symbol.Name}' is missing property '{missing.prop}' referenced in ContractKey format string.",
                                category: "Shiny.Mediator.Contracts",
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            location));
                    }
                    continue;
                }

                // 4. Generate code
                var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
                var className = symbol.Name;
                var typeKind = decl is ClassDeclarationSyntax ? "class" : "record";
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(ns))
                {
                    sb.AppendLine($"namespace {ns};");
                }
                sb.AppendLine($"partial {typeKind} {className} : global::{IContractKeyInterface}");
                sb.AppendLine("{");
                sb.AppendLine("    public string GetKey()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var args = new object[{propertyOrder.Count}];");
                for (int i = 0; i < propertyOrder.Count; i++)
                {
                    var (prop, format) = propertyOrder[i];
                    var propSymbol = symbol.GetMembers().OfType<IPropertySymbol>().First(x => x.Name == prop);
                    var propAccess = prop;
                    var formatStringLiteral = format != null ? $"\"{format}\"" : null;
                    
                    // Check if the property type is nullable
                    var isNullable = propSymbol.NullableAnnotation == NullableAnnotation.Annotated || 
                                   propSymbol.Type.CanBeReferencedByName && propSymbol.Type.Name.EndsWith("?") ||
                                   propSymbol.Type.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T;
                    
                    // Check if it's a non-nullable value type
                    var isNonNullableValueType = propSymbol.Type.IsValueType && !isNullable;
                    
                    if (isNonNullableValueType)
                    {
                        // Non-nullable value types - no null check needed
                        if (format != null)
                        {
                            sb.AppendLine($"        args[{i}] = {propAccess}.ToString({formatStringLiteral}, System.Globalization.CultureInfo.InvariantCulture);");
                        }
                        else if (propSymbol.Type.SpecialType == SpecialType.System_DateTime)
                        {
                            sb.AppendLine($"        args[{i}] = {propAccess}.ToString(\"G\");");
                        }
                        else
                        {
                            sb.AppendLine($"        args[{i}] = {propAccess};");
                        }
                    }
                    else
                    {
                        // Nullable types (reference types or nullable value types) - need null check
                        sb.AppendLine($"        if ({propAccess} != null)");
                        if (format != null)
                        {
                            var valueAccess = isNullable && propSymbol.Type.IsValueType ? $"{propAccess}.Value" : propAccess;
                            sb.AppendLine($"            args[{i}] = {valueAccess}.ToString({formatStringLiteral}, System.Globalization.CultureInfo.InvariantCulture);");
                        }
                        else if (propSymbol.Type.SpecialType == SpecialType.System_DateTime || 
                                 (propSymbol.Type.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T && 
                                  ((INamedTypeSymbol)propSymbol.Type).TypeArguments[0].SpecialType == SpecialType.System_DateTime))
                        {
                            var valueAccess = isNullable && propSymbol.Type.IsValueType ? $"{propAccess}.Value" : propAccess;
                            sb.AppendLine($"            args[{i}] = {valueAccess}.ToString(\"{format ?? "G"}\");");
                        }
                        else
                            sb.AppendLine($"            args[{i}] = {propAccess};");
                        sb.AppendLine("        else");
                        sb.AppendLine($"            args[{i}] = string.Empty;");
                    }
                }
                sb.AppendLine($"        return string.Format(\"{BuildFormatString(formatString, propertyOrder)}\", args);");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                spc.AddSource($"{className}_ContractKey.g.cs", sb.ToString());
            }
        });
    }

    static bool IsCandidate(SyntaxNode node)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } || 
           node is RecordDeclarationSyntax { AttributeLists.Count: > 0 };

    static (MemberDeclarationSyntax Decl, SemanticModel Model)? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax cds)
        {
            foreach (var attrList in cds.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                    var typeName = symbol?.ContainingType?.ToDisplayString();
                    if (typeName == AttributeName || typeName == "Shiny.Mediator.ContractKeyAttribute" || 
                        symbol?.ContainingType?.Name == "ContractKeyAttribute")
                        return (cds, context.SemanticModel);
                }
            }
        }
        else if (context.Node is RecordDeclarationSyntax rds)
        {
            foreach (var attrList in rds.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attr).Symbol as IMethodSymbol;
                    var typeName = symbol?.ContainingType?.ToDisplayString();
                    if (typeName == AttributeName || typeName == "Shiny.Mediator.ContractKeyAttribute" || 
                        symbol?.ContainingType?.Name == "ContractKeyAttribute")
                        return (rds, context.SemanticModel);
                }
            }
        }
        return null;
    }

    static List<(string prop, string? format)> ParseFormatString(string format)
    {
        // Find all {Property[:Format]} in the format string
        var props = new List<(string, string?)>();
        int idx = 0;
        while (idx < format.Length)
        {
            int open = format.IndexOf('{', idx);
            if (open == -1) break;
            int close = format.IndexOf('}', open);
            if (close == -1) break;
            var inner = format.Substring(open + 1, close - open - 1);
            var parts = inner.Split(new[] { ':' }, 2);
            var prop = parts[0];
            string? fmt = parts.Length > 1 ? parts[1] : null;
            props.Add((prop, fmt));
            idx = close + 1;
        }
        return props;
    }

    static string BuildFormatString(string? format, List<(string prop, string? format)> propertyOrder)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            // No format string provided - create a default format with all properties separated by pipe
            var formatParts = new List<string>();
            for (int i = 0; i < propertyOrder.Count; i++)
            {
                formatParts.Add($"{{{i}}}");
            }
            return string.Join("|", formatParts);
        }
        
        // Replace {Prop[:Format]} with {index}
        int idx = 0;
        var sb = new StringBuilder();
        int last = 0;
        while (true)
        {
            int open = format!.IndexOf('{', last);
            if (open == -1)
            {
                sb.Append(format.Substring(last));
                break;
            }
            sb.Append(format.Substring(last, open - last));
            int close = format.IndexOf('}', open);
            if (close == -1) break;
            sb.Append($"{{{idx}}}");
            idx++;
            last = close + 1;
        }
        return sb.ToString();
    }
}