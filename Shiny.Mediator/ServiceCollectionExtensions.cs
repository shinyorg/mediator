using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Impl;

namespace Shiny.Mediator;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShinyMediator(this IServiceCollection services)
    {
        services.TryAddSingleton<IMediator, Impl.Mediator>();
        services.AddSingleton<EventCollector>();
        services.AddScoped(typeof(MediatorEventHandler<>));
        return services;
    }
}