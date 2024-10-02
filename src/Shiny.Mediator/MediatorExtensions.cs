using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class MediatorExtensions
{
    /// <summary>
    /// Fire & Forget task pattern
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onError"></param>
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Fire & Forget task pattern that logs errors
    /// </summary>
    /// <param name="task"></param>
    /// <param name="errorLogger"></param>
    public static void RunInBackground(this Task task, ILogger errorLogger)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                errorLogger.LogError(x.Exception, "Fire & Forget trapped error");
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Add Shiny Mediator to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static IServiceCollection AddShinyMediator(this IServiceCollection services, Action<ShinyConfigurator>? configurator = null)
    {
        var cfg = new ShinyConfigurator(services);
        configurator?.Invoke(cfg);
        if (!cfg.ExcludeDefaultMiddleware)
        {
            cfg.AddHttpClient();
            cfg.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
            cfg.AddEventExceptionHandlingMiddleware();
            cfg.AddPerformanceLoggingMiddleware();
        }

        services.TryAddSingleton<IMediator, Infrastructure.Impl.Mediator>();
        services.TryAddSingleton<IRequestSender, DefaultRequestSender>();
        services.TryAddSingleton<IEventPublisher, DefaultEventPublisher>();
        return services;
    }
    
    
    /// <summary>
    /// Performance logging middleware
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddPerformanceLoggingMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenRequestMiddleware(typeof(PerformanceLoggingRequestMiddleware<,>));


    /// <summary>
    ///  Event Exception Management
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddEventExceptionHandlingMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenEventMiddleware(typeof(ExceptionHandlerEventMiddleware<>));
    
    
    /// <summary>
    /// Adds data annotation validation to your contracts & request handlers
    /// </summary>
    /// <param name="configurator"></param>
    /// <returns></returns>
    public static ShinyConfigurator AddDataAnnotations(this ShinyConfigurator configurator)
        => configurator.AddOpenRequestMiddleware(typeof(DataAnnotationsRequestMiddleware<,>));
    
    
    /// <summary>
    /// Transforms result to a timestamped values
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="result"></param>
    /// <param name="dt"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static TimestampedResult<TResult> ToTimestamp<TRequest, TResult>(this IRequestHandler<TRequest, TResult> handler, TResult result, DateTimeOffset? dt = null) where TRequest : IRequest<TResult>
        => new (dt ?? DateTimeOffset.UtcNow, result);

    
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

    
    public static ShinyConfigurator AddTimerRefreshStreamMiddleware(this ShinyConfigurator cfg)
        => cfg.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
}