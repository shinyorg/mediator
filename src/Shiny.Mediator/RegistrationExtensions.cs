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
    extension(ShinyMediatorBuilder mediatorBuilder)
    {
        /// <summary>
        /// Adds command scheduling
        /// </summary>
        /// <typeparam name="TScheduler">The scheduler/execution type for deferred/scheduled commands</typeparam>
        /// <returns></returns>
        public ShinyMediatorBuilder AddCommandScheduling<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TScheduler>()
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
        /// <returns></returns>
        public ShinyMediatorBuilder AddInMemoryCommandScheduling()
            => mediatorBuilder.AddCommandScheduling<InMemoryCommandScheduler>();
        
        /// <summary>
        /// Performance logging middleware
        /// </summary>
        /// <returns></returns>
        public ShinyMediatorBuilder AddPerformanceLoggingMiddleware()
        {
            mediatorBuilder.AddOpenRequestMiddleware(typeof(PerformanceLoggingRequestMiddleware<,>));
            mediatorBuilder.AddOpenCommandMiddleware(typeof(PerformanceLoggingCommandMiddleware<>));
            return mediatorBuilder;
        }


        /// <summary>
        /// Add global exception handler
        /// </summary>
        /// <param name="lifetime"></param>
        /// <typeparam name="THandler"></typeparam>
        /// <returns></returns>
        public ShinyMediatorBuilder AddExceptionHandler<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler
        >(
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
        /// <returns></returns>
        public ShinyMediatorBuilder PreventEventExceptions()
            => mediatorBuilder.AddExceptionHandler<EventExceptionHandler>();
        
        
        /// <summary>
        /// Adds data annotation validation to your contracts, request handlers, & command handlers
        /// </summary>
        /// <returns></returns>
        public ShinyMediatorBuilder AddDataAnnotations()
        {
            mediatorBuilder.AddOpenRequestMiddleware(typeof(DataAnnotationsRequestMiddleware<,>));
            mediatorBuilder.AddOpenCommandMiddleware(typeof(DataAnnotationsCommandMiddleware<>));
            return mediatorBuilder;
        }


        /// <summary>
        /// Adds queued event middleware that supports both sampling (fixed-window, last-event-wins) and
        /// throttling (first-event-executes, cooldown-discards) via [Sample] and [Throttle] attributes.
        /// </summary>
        /// <returns></returns>
        public ShinyMediatorBuilder AddQueuedEventMiddleware()
            => mediatorBuilder.AddOpenEventMiddleware(typeof(QueuedEventMiddleware<>), ServiceLifetime.Singleton);


        /// <summary>
        /// Adds timer calling for async enumerables
        /// </summary>
        /// <returns></returns>
        public ShinyMediatorBuilder AddTimerRefreshStreamMiddleware()
            => mediatorBuilder.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
    }


    extension(IServiceCollection services)
    {
        /// <summary>
        /// Add Shiny Mediator to the service collection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configurator"></param>
        /// <param name="includeStandardMiddleware">By default, we will include </param>
        /// <returns></returns>
        public IServiceCollection AddShinyMediator(
            Action<ShinyMediatorBuilder>? configurator = null,
            bool includeStandardMiddleware = true
        )
        {
            var cfg = new ShinyMediatorBuilder(services);
            configurator?.Invoke(cfg);

            if (includeStandardMiddleware)
            {
                cfg.AddHttpClientServices();
                cfg.PreventEventExceptions();
                cfg.AddTimerRefreshStreamMiddleware();
                cfg.AddQueuedEventMiddleware();
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
        /// Registers a type as itself and all of its implemented interfaces with a scoped lifetime. If the type is already registered, it will not be registered again.
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IServiceCollection AddSingletonAsImplementedInterfaces<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors | 
                DynamicallyAccessedMemberTypes.NonPublicConstructors | 
                DynamicallyAccessedMemberTypes.Interfaces
            )] TImplementation
        >() where TImplementation : class
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
        
        
        /// <summary>
        /// Registers the implementation as itself and all of its implemented interfaces with a scoped lifetime. If the implementation is already registered, it will not be registered again.
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IServiceCollection AddScopedAsImplementedInterfaces<
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicConstructors | 
                DynamicallyAccessedMemberTypes.NonPublicConstructors | 
                DynamicallyAccessedMemberTypes.Interfaces
            )] TImplementation
        >() where TImplementation : class
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
}