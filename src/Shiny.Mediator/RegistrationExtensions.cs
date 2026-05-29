using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Mediator.Http;
using Shiny.Mediator.Infrastructure;
using Shiny.Mediator.Infrastructure.Impl;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


/// <summary>
/// Registration helpers for Shiny Mediator on top of <see cref="IServiceCollection"/> and
/// <see cref="ShinyMediatorBuilder"/> - wiring up the mediator itself and the built-in middleware.
/// </summary>
public static class RegistrationExtensions
{
    extension(ShinyMediatorBuilder mediatorBuilder)
    {
        /// <summary>
        /// Registers <typeparamref name="TScheduler"/> as the <see cref="ICommandScheduler"/> and
        /// installs the scheduled-command middleware so commands with a due-date are deferred instead of run immediately.
        /// </summary>
        /// <typeparam name="TScheduler">The scheduler implementation that holds and executes deferred commands.</typeparam>
        public ShinyMediatorBuilder AddCommandScheduling<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TScheduler>()
            where TScheduler : class, ICommandScheduler
        {
            mediatorBuilder.Services.TryAddSingleton<ICommandScheduler, TScheduler>();
            mediatorBuilder.Services.TryAddSingleton(TimeProvider.System);
            mediatorBuilder.AddOpenCommandMiddleware(typeof(ScheduledCommandMiddleware<>));
            return mediatorBuilder;
        }

        /// <summary>
        /// Registers the built-in in-memory <see cref="ICommandScheduler"/>. Suitable for testing or single-process scenarios
        /// where deferred commands do not need to survive process restarts.
        /// </summary>
        public ShinyMediatorBuilder AddInMemoryCommandScheduling()
            => mediatorBuilder.AddCommandScheduling<InMemoryCommandScheduler>();

        /// <summary>
        /// Installs the performance-logging middleware for both requests and commands. Threshold is read from configuration.
        /// </summary>
        public ShinyMediatorBuilder AddPerformanceLoggingMiddleware()
        {
            mediatorBuilder.AddOpenRequestMiddleware(typeof(PerformanceLoggingRequestMiddleware<,>));
            mediatorBuilder.AddOpenCommandMiddleware(typeof(PerformanceLoggingCommandMiddleware<>));
            return mediatorBuilder;
        }


        /// <summary>
        /// Registers a global <see cref="IExceptionHandler"/> that participates in the exception-handling chain
        /// for every mediator execution.
        /// </summary>
        /// <param name="lifetime">Either <see cref="ServiceLifetime.Singleton"/> or <see cref="ServiceLifetime.Scoped"/>.</param>
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
        /// Installs an <see cref="IExceptionHandler"/> that swallows exceptions thrown from event handlers and
        /// logs them, preventing event publication from propagating failures to the caller.
        /// </summary>
        public ShinyMediatorBuilder PreventEventExceptions()
            => mediatorBuilder.AddExceptionHandler<EventExceptionHandler>();


        /// <summary>
        /// Installs middleware that runs <see cref="System.ComponentModel.DataAnnotations"/> validation against
        /// requests and commands marked with <c>[Validate]</c>, throwing <c>ValidateException</c> when invalid.
        /// </summary>
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
        public ShinyMediatorBuilder AddQueuedEventMiddleware()
            => mediatorBuilder.AddOpenEventMiddleware(typeof(QueuedEventMiddleware<>), ServiceLifetime.Singleton);

        /// <summary>
        /// Installs middleware that periodically re-invokes a stream handler based on <see cref="TimerRefreshAttribute"/>
        /// or configuration, producing fresh values on a fixed interval.
        /// </summary>
        public ShinyMediatorBuilder AddTimerRefreshStreamMiddleware()
            => mediatorBuilder.AddOpenStreamMiddleware(typeof(TimerRefreshStreamRequestMiddleware<,>));
    }


    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers Shiny Mediator and its core services on the service collection.
        /// </summary>
        /// <param name="configurator">Optional callback to add handlers, middleware, and extensions.</param>
        /// <param name="includeStandardMiddleware">When true (default), installs the standard middleware set:
        /// HTTP client services, event-exception trapping, timer-refresh stream middleware, and queued event middleware.</param>
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
    }
}
