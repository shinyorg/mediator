using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;


[Generator]
public class MediatorSourceGenerator : ISourceGenerator
{
    // SyntaxReceiver syntaxReceiver = new ClassesWithInterfacesReceiver("IEnumerable");
    public void Initialize(GeneratorInitializationContext context)
    {
        // context.RegisterForSyntaxNotifications(() => syntaxReceiver);
    }

    
    public void Execute(GeneratorExecutionContext context)
    {
        // if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
        // {
        //     return;
        // }
        // foreach (INamedTypeSymbol classSymbol in this.syntaxReceiver.Classes)
        // {
        //     // process your class here.
        // }
    }
}