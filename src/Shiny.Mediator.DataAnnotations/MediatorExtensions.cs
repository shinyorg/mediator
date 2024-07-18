using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    public static ShinyConfigurator AddDataAnnotations(this ShinyConfigurator configurator)
    {
        // configurator.Services.AddSingleton<IMediatorHandler, DataAnnotationMediatorHandler>();
        return configurator;
    }    
}