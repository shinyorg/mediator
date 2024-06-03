using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Impl;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    public static void RunOffThread(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        });

    
    public static IServiceCollection AddShinyMediator(this IServiceCollection services, Action<ShinyConfigurator>? configurator = null)
    {
        services.TryAddSingleton<IMediator, Impl.Mediator>();
        services.TryAddSingleton<IRequestSender, DefaultRequestSender>();
        services.TryAddSingleton<IEventPublisher, DefaultEventPublisher>();
        configurator?.Invoke(new ShinyConfigurator(services));

        return services;
    }

    
    public static ShinyConfigurator AddExceptionHandling(this ShinyConfigurator cfg)
        => cfg.AddOpenRequestMiddleware(typeof(ExceptionHandlerMiddleware<,>));

    public static ShinyConfigurator AddTimedMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenRequestMiddleware(typeof(TimedMiddleware<,>));

    
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