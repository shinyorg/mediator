using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.Mediator;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShinyMediator(this IServiceCollection services, Action<ShinyMediatorConfiguration> configurator)
    {
        services.TryAddSingleton<IMediator, Impl.Mediator>();
        return services;
    }
}