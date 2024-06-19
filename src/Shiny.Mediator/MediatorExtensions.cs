using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Impl;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    
    public static IServiceCollection AddShinyMediator(this IServiceCollection services, Action<ShinyConfigurator>? configurator = null)
    {
        configurator?.Invoke(new ShinyConfigurator(services));
        services.TryAddSingleton<IMediator, Impl.Mediator>();
        services.TryAddSingleton<IRequestSender, DefaultRequestSender>();
        services.TryAddSingleton<IEventPublisher, DefaultEventPublisher>();
        return services;
    }
    
    
    public static TAttribute? GetHandlerHandleMethodAttribute<TRequest, TAttribute>(this IRequestHandler handler) where TAttribute : Attribute
        => handler
            .GetType()
            .GetMethod(
                "Handle", 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                CallingConventions.Any,
                [ typeof(TRequest), typeof(CancellationToken) ],
                null
            )!
            .GetCustomAttribute<TAttribute>();
    
    
    public static TAttribute? GetHandlerHandleMethodAttribute<TEvent, TAttribute>(this IEventHandler<TEvent> handler) 
        where TEvent : IEvent
        where TAttribute : Attribute
        => handler
            .GetType()
            .GetMethod(
                "Handle", 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                CallingConventions.Any,
                [ typeof(TEvent), typeof(CancellationToken) ],
                null
            )!
            .GetCustomAttribute<TAttribute>();

    
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


    public static ShinyConfigurator AddReplayStreamMiddleware(this ShinyConfigurator configurator)
    {
        configurator.AddOpenStreamMiddleware(typeof(ReplayStreamMiddleware<,>));
        return configurator;
    }
}