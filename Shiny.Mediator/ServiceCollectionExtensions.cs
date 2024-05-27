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


    public static IServiceCollection AddSingletonAsImplementedInterfaces<TImplementation>(this IServiceCollection services) where TImplementation : class
    {
        var interfaceTypes = typeof(TImplementation).GetInterfaces();
        if (interfaceTypes.Length == 0)
            throw new InvalidOperationException(services.GetType().FullName + " does not implement any interfaces");

        services.AddSingleton<TImplementation>();
        foreach (var interfaceType in interfaceTypes)
            services.AddSingleton(interfaceType, sp => sp.GetRequiredService<TImplementation>());

        return services;
    }
    
    
    public static IServiceCollection AddScopedAsImplementedInterfaces<TImplementation>(this IServiceCollection services) where TImplementation : class
    {
        var interfaceTypes = typeof(TImplementation).GetInterfaces();
        if (interfaceTypes.Length == 0)
            throw new InvalidOperationException(services.GetType().FullName + " does not implement any interfaces");

        services.AddScoped<TImplementation>();
        foreach (var interfaceType in interfaceTypes)
            services.AddScoped(interfaceType, sp => sp.GetRequiredService<TImplementation>());

        return services;
    }
}