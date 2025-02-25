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
        Action<ShinyConfigurator>? configurator = null,
        bool includeStandardMiddleware = true
    )
    {
        var cfg = new ShinyConfigurator(services);
        configurator?.Invoke(cfg);

        if (includeStandardMiddleware)
        {
            cfg.AddHttpClient();
            cfg.AddGlobalExceptionHandling();
            cfg.PreventEventExceptions();
            cfg.AddPerformanceLoggingMiddleware();
            cfg.AddTimerRefreshStreamMiddleware();
        }
        
        services.TryAddSingleton<ISerializerService, SysTextJsonSerializerService>();
        services.TryAddSingleton<IRequestExecutor, RequestExecutor>();
        services.TryAddSingleton<IStreamRequestExecutor, StreamRequestExecutor>();
        services.TryAddSingleton<ICommandExecutor, CommandExecutor>();
        services.TryAddSingleton<IEventExecutor, EventExecutor>();
        services.TryAddSingleton<IMediator, Infrastructure.Impl.Mediator>();
        services.TryAddSingleton(TimeProvider.System);
        
        return services;
    }
    

    /// <summary>
    /// Add HTTP Client to mediator
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddHttpClient(this ShinyConfigurator configurator)
    {
        configurator.Services.AddScoped(typeof(IRequestHandler<,>), typeof(HttpRequestHandler<,>));
        return configurator;
    }
    

    /// <summary>
    /// Adds command scheduling
    /// </summary>
    /// <param name="configurator"></param>
    /// <typeparam name="TScheduler">The scheduler/execution type for deferred/scheduled commands</typeparam>
    /// <returns></returns>
    public static ShinyConfigurator AddCommandScheduling<TScheduler>(this ShinyConfigurator configurator)
        where TScheduler : class, ICommandScheduler
    {
        configurator.Services.AddSingleton<ICommandScheduler, TScheduler>();
        configurator.AddOpenCommandMiddleware(typeof(ScheduledCommandMiddleware<>));
        configurator.Services.TryAddSingleton(TimeProvider.System);
        return configurator;
    }


    /// <summary>
    /// Adds in-memory command scheduling
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddInMemoryCommandScheduling(this ShinyConfigurator configurator)
        => configurator.AddCommandScheduling<Infrastructure.Impl.InMemoryCommandScheduler>();


    /// <summary>
    /// Performance logging middleware
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddPerformanceLoggingMiddleware(this ShinyConfigurator cfg)
    {
        cfg.AddOpenRequestMiddleware(typeof(PerformanceLoggingRequestMiddleware<,>));
        cfg.AddOpenCommandMiddleware(typeof(PerformanceLoggingCommandMiddleware<>));
        return cfg;
    }


    /// <summary>
    /// Add global exception handler
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="lifetime"></param>
    /// <typeparam name="THandler"></typeparam>
    /// <returns></returns>
    public static ShinyConfigurator AddExceptionHandler<THandler>(
        this ShinyConfigurator configurator,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) where THandler : class, IExceptionHandler
    {
        configurator.AddGlobalExceptionHandling();
        if (lifetime == ServiceLifetime.Singleton)
            configurator.Services.AddSingleton<IExceptionHandler, THandler>();
        else
            configurator.Services.AddScoped<IExceptionHandler, THandler>();

        return configurator;
    }

    
    /// <summary>
    /// Adds global exception handling this logs errors in an event handler without allowing it to crash out your app
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator PreventEventExceptions(this ShinyConfigurator configurator)
        => configurator.AddExceptionHandler<EventExceptionHandler>(ServiceLifetime.Singleton);
    
    
    /// <summary>
    /// Add global exception handling
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddGlobalExceptionHandling(this ShinyConfigurator configurator)
    {
        if (!configurator.Services.Any(x => x.ImplementationType == typeof(GlobalExceptionHandler)))
        {
            configurator.Services.AddScoped<GlobalExceptionHandler>();
            configurator.AddOpenCommandMiddleware(typeof(ExceptionHandlingCommandMiddleware<>));
            configurator.AddOpenEventMiddleware(typeof(ExceptionHandlingEventMiddleware<>));
            configurator.AddOpenRequestMiddleware(typeof(ExceptionHandlingRequestMiddleware<,>));
        }
        return configurator;
    }
    
    
    /// <summary>
    /// Adds data annotation validation to your contracts, request handlers, & command handlers
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddDataAnnotations(this ShinyConfigurator configurator)
    {
        configurator.AddOpenRequestMiddleware(typeof(DataAnnotationsRequestMiddleware<,>));
        configurator.AddOpenCommandMiddleware(typeof(DataAnnotationsCommandMiddleware<>));
        return configurator;
    }


    /// <summary>
    /// Adds timer calling for async enumerables
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddTimerRefreshStreamMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
    
    
    public static IServiceCollection AddSingletonAsImplementedInterfaces<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors | 
            DynamicallyAccessedMemberTypes.NonPublicConstructors | 
            DynamicallyAccessedMemberTypes.Interfaces)
        ] TImplementation
    >(this IServiceCollection services) where TImplementation : class
    {
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
        var interfaceTypes = typeof(TImplementation).GetInterfaces();
        if (interfaceTypes.Length == 0)
            throw new InvalidOperationException(services.GetType().FullName + " does not implement any interfaces");

        services.AddScoped<TImplementation>();
        foreach (var interfaceType in interfaceTypes)
            services.AddScoped(interfaceType, sp => sp.GetRequiredService<TImplementation>());

        return services;
    }
}