using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class AttributeMarkerSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes that implement handler interfaces
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsHandlerCandidate(s),
                transform: static (ctx, _) => GetHandlerWithAttributes(ctx)
            )
            .Where(static m => m is not null);

        // Generate source for each handler class
        context.RegisterSourceOutput(handlerClasses, static (spc, handler) => 
            GenerateAttributeMarker(spc, handler!));
    }

    static bool IsHandlerCandidate(SyntaxNode node)
    {
        // Look for classes that might be handlers
        return node is ClassDeclarationSyntax;
    }

    static HandlerAttributeInfo? GetHandlerWithAttributes(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl);
        
        if (symbol is not INamedTypeSymbol classSymbol)
            return null;

        // Skip if already implements IHandlerAttributeMarker (already generated)
        if (classSymbol.AllInterfaces.Any(i => i.Name == "IHandlerAttributeMarker"))
            return null;

        // Check if this class implements any of the handler interfaces
        var handlerInterfaces = new List<(INamedTypeSymbol interfaceSymbol, ITypeSymbol messageType)>();
        
        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (iface.Name == "IRequestHandler" && iface.TypeArguments.Length == 2)
            {
                handlerInterfaces.Add((iface, iface.TypeArguments[0]));
            }
            else if (iface.Name == "IStreamRequestHandler" && iface.TypeArguments.Length == 2)
            {
                handlerInterfaces.Add((iface, iface.TypeArguments[0]));
            }
            else if (iface.Name == "ICommandHandler" && iface.TypeArguments.Length == 1)
            {
                handlerInterfaces.Add((iface, iface.TypeArguments[0]));
            }
            else if (iface.Name == "IEventHandler" && iface.TypeArguments.Length == 1)
            {
                handlerInterfaces.Add((iface, iface.TypeArguments[0]));
            }
        }

        if (handlerInterfaces.Count == 0)
            return null;

        // Find Handle methods with attributes
        var attributesByMessageType = new Dictionary<string, List<AttributeData>>();

        foreach (var (_, messageType) in handlerInterfaces)
        {
            // Find the Handle method implementation
            var handleMethod = classSymbol.GetMembers("Handle")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length > 0 && 
                                     SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, messageType));

            if (handleMethod == null)
                continue;

            // Get attributes that inherit from MediatorMiddlewareAttribute
            var mediatorAttributes = handleMethod.GetAttributes()
                .Where(attr => InheritsFromMediatorMiddlewareAttribute(attr.AttributeClass))
                .ToList();

            if (mediatorAttributes.Count > 0)
            {
                var messageTypeFullName = messageType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "");
                
                if (!attributesByMessageType.ContainsKey(messageTypeFullName))
                {
                    attributesByMessageType[messageTypeFullName] = new List<AttributeData>();
                }
                
                attributesByMessageType[messageTypeFullName].AddRange(mediatorAttributes);
            }
        }

        if (attributesByMessageType.Count == 0)
            return null;

        // Check if class is partial
        var isPartial = classDecl.Modifiers.Any(m => m.Text == "partial");
        
        return new HandlerAttributeInfo(
            ClassSymbol: classSymbol,
            ClassDeclaration: classDecl,
            AttributesByMessageType: attributesByMessageType,
            IsPartial: isPartial
        );
    }

    static bool InheritsFromMediatorMiddlewareAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
            return false;

        var current = attributeClass;
        while (current != null)
        {
            if (current.Name == "MediatorMiddlewareAttribute")
                return true;
            current = current.BaseType;
        }
        return false;
    }

    static void GenerateAttributeMarker(SourceProductionContext context, HandlerAttributeInfo handler)
    {
        // Report diagnostic if class is not partial
        if (!handler.IsPartial)
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "SHINY001",
                    title: "Handler must be partial",
                    messageFormat: "Handler class '{0}' must be declared as partial to use middleware attribute marker on methods",
                    category: "ShinyMediator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                handler.ClassDeclaration.Identifier.GetLocation(),
                handler.ClassSymbol.Name);
            
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var sb = new StringBuilder();
        
        // File header
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// Code generated by Shiny Mediator Source Generator.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"using System.Linq;");

        // Namespace
        var namespaceName = handler.ClassSymbol.ContainingNamespace.ToDisplayString();
        if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
        {
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
        }

        // Generate partial class
        var className = handler.ClassSymbol.Name;
        var accessibility = handler.ClassSymbol.DeclaredAccessibility.ToString().ToLower();
        
        sb.AppendLine($"    {accessibility} partial class {className} : global::Shiny.Mediator.IHandlerAttributeMarker");
        sb.AppendLine("    {");
        
        // Generate attributes dictionary
        sb.AppendLine("        readonly global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<object>> __attributeMarkers = new()");
        sb.AppendLine("        {");
        
        foreach (var kvp in handler.AttributesByMessageType)
        {
            var messageType = kvp.Key;
            var attributes = kvp.Value;
            
            sb.Append($"            {{ \"{messageType}\", [");
            
            var attributeStrings = new List<string>();
            foreach (var attr in attributes)
            {
                attributeStrings.Add(GenerateAttributeInstantiation(attr));
            }
            
            sb.Append(string.Join(", ", attributeStrings));
            sb.AppendLine("] },");
        }
        
        sb.AppendLine("        };");
        sb.AppendLine();
        
        // Generate GetAttribute method
        sb.AppendLine("        public T? GetAttribute<T>(object message) where T : global::Shiny.Mediator.MediatorMiddlewareAttribute");
        sb.AppendLine("        {");
        sb.AppendLine("            var key = message.GetType().FullName!;");
        sb.AppendLine("            if (this.__attributeMarkers.TryGetValue(key, out var attributes))");
        sb.AppendLine("                return attributes.OfType<T>().FirstOrDefault()!;");
        sb.AppendLine();
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        
        sb.AppendLine("    }");
        
        if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
        {
            sb.AppendLine("}");
        }

        var fileName = $"{handler.ClassSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")}_AttributeMarker.g.cs";
        context.AddSource(fileName, sb.ToString());
    }

    static string GenerateAttributeInstantiation(AttributeData attr)
    {
        var sb = new StringBuilder();
        var attrTypeName = attr.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        sb.Append($"new {attrTypeName}");
        
        // Handle constructor arguments (indexed parameters)
        if (attr.ConstructorArguments.Length > 0)
        {
            sb.Append("(");
            var args = new List<string>();
            foreach (var arg in attr.ConstructorArguments)
            {
                args.Add(FormatTypedConstant(arg));
            }
            sb.Append(string.Join(", ", args));
            sb.Append(")");
        }
        else
        {
            sb.Append("()");
        }
        
        // Handle named arguments (properties)
        if (attr.NamedArguments.Length > 0)
        {
            sb.Append(" { ");
            var props = new List<string>();
            foreach (var namedArg in attr.NamedArguments)
            {
                props.Add($"{namedArg.Key} = {FormatTypedConstant(namedArg.Value)}");
            }
            sb.Append(string.Join(", ", props));
            sb.Append(" }");
        }
        
        return sb.ToString();
    }

    static string FormatTypedConstant(TypedConstant constant)
    {
        if (constant.IsNull)
            return "null";

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => constant.Type?.SpecialType == SpecialType.System_String 
                ? $"\"{constant.Value}\"" 
                : constant.Value?.ToString() ?? "null",
            TypedConstantKind.Enum => $"({constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){constant.Value}",
            TypedConstantKind.Type => $"typeof({((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})",
            TypedConstantKind.Array => FormatArrayConstant(constant),
            _ => constant.Value?.ToString() ?? "null"
        };
    }

    static string FormatArrayConstant(TypedConstant constant)
    {
        var elements = constant.Values.Select(FormatTypedConstant);
        return $"new[] {{ {string.Join(", ", elements)} }}";
    }

    record HandlerAttributeInfo(
        INamedTypeSymbol ClassSymbol,
        ClassDeclarationSyntax ClassDeclaration,
        Dictionary<string, List<AttributeData>> AttributesByMessageType,
        bool IsPartial
    );
}