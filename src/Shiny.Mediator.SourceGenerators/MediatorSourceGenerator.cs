using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SourceGeneratorsKit;

namespace Shiny.Mediator.SourceGenerators;


[Generator]
public class MediatorSourceGenerator : ISourceGenerator
{
    readonly SyntaxReceiver syntaxReceiver = new RegisterHandlerAttributeSyntaxReceiver();
    // SyntaxReceiver syntaxReceiver = new ClassesWithInterfacesReceiver("IEnumerable");
    
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(x => x.AddSource("RegisterHandlerAttribute.g.cs", SourceText.From(
            """
            namespace Shiny.Mediator
            {
                [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
                public class RegisterHandlerAttribute : System.Attribute {}
            }
            """
        )));
       // context.RegisterForSyntaxNotifications(() => syntaxReceiver);
    }

    
    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
            return;

        // TODO: detect double registration of request handlers
        var sb = new StringBuilder();
        sb
            .AppendLine("namespace Shiny.Mediator;")
            .AppendLine()
            .AppendLine("public static class __ShinyMediatorSourceGenExtensions {")
            .AppendLine(
                "\tpublic static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddDiscoveredMediatorHandlers(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services) {");
            
        foreach (var classSymbol in this.syntaxReceiver.Classes)
            sb.AppendLine($"\t\tservices.AddSingletonAsImplementedInterfaces<{classSymbol.ToDisplayString()}>();");

        sb
            .AppendLine("\treturn services;")
            .AppendLine("\t}")
            .AppendLine("}");

        context.AddSource("__MediatorHandlersRegistration.g.cs", SourceText.From(sb.ToString()));
    }
}