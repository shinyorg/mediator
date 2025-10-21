using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class RegistrationExtensions
{
    /// <summary>
    /// Add Shiny Mediator to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurator"></param>
    /// <param name="includeStandardMiddleware">By default, we will include </param>
    /// <returns></returns>
    public static IServiceCollection AddShinyMediator(
        this IServiceCollection services, 
        Action<ShinyMediatorBuilder>? configurator = null,
        bool includeStandardMiddleware = true
    )
    {
        var cfg = new ShinyMediatorBuilder(services);
        configurator?.Invoke(cfg);

        if (includeStandardMiddleware)
        {
            cfg.AddHttpClient();
            cfg.PreventEventExceptions();
            cfg.AddTimerRefreshStreamMiddleware();
        }
        services.TryAddSingleton<RuntimeEventRegister>();
        services.TryAddSingleton<ISerializerService, SysTextJsonSerializerService>();
        services.TryAddSingleton<IMediatorDirector, MediatorDirector>();
        services.TryAddSingleton<IContractKeyProvider, DefaultContractKeyProvider>();
        services.TryAddSingleton<IMediator, MediatorImpl>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
    

    /// <summary>
    /// Add HTTP Client to mediator
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddHttpClient(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.Services.AddHttpClient();
        mediatorBuilder.Services.AddScoped(typeof(IRequestHandler<,>), typeof(HttpRequestHandler<,>));
        mediatorBuilder.Services.AddSingleton<IRequestHandler<HttpDirectRequest, object?>, HttpDirectRequestHandler>();
        return mediatorBuilder;
    }
    

    /// <summary>
    /// Adds command scheduling
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <typeparam name="TScheduler">The scheduler/execution type for deferred/scheduled commands</typeparam>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddCommandScheduling<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TScheduler>(this ShinyMediatorBuilder mediatorBuilder)
        where TScheduler : class, ICommandScheduler
    {
        mediatorBuilder.Services.TryAddSingleton<ICommandScheduler, TScheduler>();
        mediatorBuilder.Services.TryAddSingleton(TimeProvider.System);
        mediatorBuilder.AddOpenCommandMiddleware(typeof(ScheduledCommandMiddleware<>));
        return mediatorBuilder;
    }


    /// <summary>
    /// Adds in-memory command scheduling
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddInMemoryCommandScheduling(this ShinyMediatorBuilder mediatorBuilder)
        => mediatorBuilder.AddCommandScheduling<InMemoryCommandScheduler>();


    /// <summary>
    /// Performance logging middleware
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddPerformanceLoggingMiddleware(this ShinyMediatorBuilder cfg)
    {
        cfg.AddOpenRequestMiddleware(typeof(PerformanceLoggingRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(PerformanceLoggingCommandMiddleware<>));
        return cfg;
    }


    /// <summary>
    /// Add global exception handler
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <param name="lifetime"></param>
    /// <typeparam name="THandler"></typeparam>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddExceptionHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
    >(
        this ShinyMediatorBuilder mediatorBuilder,
        ServiceLifetime lifetime = ServiceLifetime.Singleton
    ) where THandler : class, IExceptionHandler
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
                mediatorBuilder.Services.AddSingleton<IExceptionHandler, THandler>();
                break;
            
            case ServiceLifetime.Scoped:
                mediatorBuilder.Services.AddScoped<IExceptionHandler, THandler>();
                break;
            
            default:
                throw new InvalidOperationException($"Invalid Lifetime for ExceptionHandler: {lifetime}");
        }

        
        return mediatorBuilder;
    }

    
    /// <summary>
    /// Adds global exception handling this logs errors in an event handler without allowing it to crash out your app
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder PreventEventExceptions(this ShinyMediatorBuilder mediatorBuilder)
        => mediatorBuilder.AddExceptionHandler<EventExceptionHandler>();
    
    
    /// <summary>
    /// Adds data annotation validation to your contracts, request handlers, & command handlers
    /// </summary>
    /// <param name="mediatorBuilder"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddDataAnnotations(this ShinyMediatorBuilder mediatorBuilder)
    {
        mediatorBuilder.AddOpenRequestMiddleware(typeof(DataAnnotationsRequestMiddleware<,>));
        mediatorBuilder.AddOpenCommandMiddleware(typeof(DataAnnotationsCommandMiddleware<>));
        return mediatorBuilder;
    }


    /// <summary>
    /// Adds timer calling for async enumerables
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyMediatorBuilder AddTimerRefreshStreamMiddleware(this ShinyMediatorBuilder cfg)
        => cfg.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
    
    
    public static IServiceCollection AddSingletonAsImplementedInterfaces<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | 
            DynamicallyAccessedMemberTypes.NonPublicConstructors | 
            DynamicallyAccessedMemberTypes.Interfaces
        )] TImplementation
    >(this IServiceCollection services) where TImplementation : class
    {
        // check if implementation is already registered and ignore if it is
        if (services.Any(x => x.ServiceType == typeof(TImplementation)))
            return services;
        
        var interfaceTypes = typeof(TImplementation).GetInterfaces();
        if (interfaceTypes.Length == 0)
            throw new InvalidOperationException(services.GetType().FullName + " does not implement any interfaces");

        services.AddSingleton<TImplementation>();
        foreach (var interfaceType in interfaceTypes)
            services.AddSingleton(interfaceType, sp => sp.GetRequiredService<TImplementation>());

        return services;
    }
    
    
    public static IServiceCollection AddScopedAsImplementedInterfaces<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | 
            DynamicallyAccessedMemberTypes.NonPublicConstructors | 
            DynamicallyAccessedMemberTypes.Interfaces
        )] TImplementation
    >(this IServiceCollection services) where TImplementation : class
    {
        // check if implementation is already registered and ignore if it is
        if (services.Any(x => x.ServiceType == typeof(TImplementation)))
            return services;
        
        var interfaceTypes = typeof(TImplementation).GetInterfaces();
        if (interfaceTypes.Length == 0)
            throw new InvalidOperationException(services.GetType().FullName + " does not implement any interfaces");

        services.AddScoped<TImplementation>();
        foreach (var interfaceType in interfaceTypes)
            services.AddScoped(interfaceType, sp => sp.GetRequiredService<TImplementation>());

        return services;
    }
}