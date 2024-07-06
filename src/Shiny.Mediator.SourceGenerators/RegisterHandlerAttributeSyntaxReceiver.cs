using System;
using Microsoft.CodeAnalysis;
using SourceGeneratorsKit;

namespace Shiny.Mediator.SourceGenerators;


public class RegisterHandlerAttributeSyntaxReceiver : SyntaxReceiver
{
    public override bool CollectClassSymbol => true;

    protected override bool ShouldCollectClassSymbol(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.ContainingAssembly.Name.StartsWith("Shiny.Mediator", StringComparison.CurrentCultureIgnoreCase))
            return false;
        
        var hasAttribute = classSymbol.HasAttribute("SingletonHandlerAttribute");
        if (hasAttribute)
            return true;
        
        hasAttribute = classSymbol.HasAttribute("ScopedHandlerAttribute");
        if (hasAttribute)
            return true;
        
        hasAttribute = classSymbol.HasAttribute("SingletonMiddlewareAttribute");
        if (hasAttribute)
            return true;

        hasAttribute = classSymbol.HasAttribute("ScopedMiddlewareAttribute");
        if (hasAttribute)
            return true;
        
        return false;
    }
}