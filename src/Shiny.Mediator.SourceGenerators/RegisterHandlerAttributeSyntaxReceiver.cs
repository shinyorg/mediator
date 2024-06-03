using Microsoft.CodeAnalysis;
using SourceGeneratorsKit;

namespace Shiny.Mediator.SourceGenerators;


public class RegisterHandlerAttributeSyntaxReceiver : SyntaxReceiver
{
    public override bool CollectClassSymbol => true;

    protected override bool ShouldCollectClassSymbol(INamedTypeSymbol classSymbol)
    {
        var hasAttribute = classSymbol.HasAttribute("RegisterHandlerAttribute");
        if (hasAttribute)
        {
            // TODO: log error
            if (classSymbol.IsImplements("Shiny.Mediator.IEventHandler`1"))
                return false;

            if (classSymbol.IsImplements("Shiny.Mediator.IRequestHandler`1"))
                return false;
        }
        hasAttribute = classSymbol.HasAttribute("RegisterMiddleware");
        if (hasAttribute)
        {
            return true;
        }
        return false;
    }
}