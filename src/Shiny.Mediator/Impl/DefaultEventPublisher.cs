using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class DefaultEventPublisher(
    IServiceProvider services, 
    IEnumerable<IEventCollector> collectors
) : IEventPublisher
{
    readonly SubscriptionEventCollector subscriptions = new();

    
    public async Task Publish<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default
    ) where TEvent : IEvent
    {
        // allow registered services to be transient/scoped/singleton
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToList();
        
        // "covariance" event publishing chain
        // typeof(TEvent).BaseType == typeof(object) // loop on basetype until object
        //typeof(TEvent).BaseType.GetInterfaces().Any(x => x == typeof(IEvent)); // must be implementing IEvent although it wouldn't have been able to compile anyhow
        //var globalHandlers = scope.ServiceProvider.GetServices<IEventHandler<IEvent>>().ToList(); // global handlers
        
        AppendHandlersIf(handlers, this.subscriptions);
        foreach (var collector in collectors)
            AppendHandlersIf(handlers, collector);

        if (handlers.Count == 0)
            return;

        var logger = services.GetRequiredService<ILogger<TEvent>>();
        var middlewares = scope.ServiceProvider.GetServices<IEventMiddleware<TEvent>>().ToList();
        var tasks = handlers
            .Select(x => Execute(@event, x, logger, middlewares, cancellationToken))
            .ToList();
        
        await Task
            .WhenAll(tasks)
            .ConfigureAwait(false);
    }

    
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
    {
        var handler = new SubscriptionEventHandler<TEvent>(this.subscriptions);
        handler.OnHandle = action;
        return handler;
    }


    static async Task Execute<TEvent>(
        TEvent @event,
        IEventHandler<TEvent> eventHandler, 
        ILogger logger,
        IEnumerable<IEventMiddleware<TEvent>> middlewares,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var handlerDelegate = new EventHandlerDelegate(() =>
        {
            logger.LogDebug(
                "Executing Event Handler {HandlerType}", 
                eventHandler.GetType().FullName
            );
            return eventHandler.Handle(@event, cancellationToken);
        });
        
        await middlewares
            .Reverse()
            .Aggregate(
                handlerDelegate, 
                (next, middleware) => () =>
                {
                    logger.LogDebug(
                        "Executing event middleware {MiddlewareType} for {RequestType}",
                        middleware.GetType().FullName,
                        @event.GetType().FullName
                    );
                    
                    return middleware.Process(
                        @event,
                        next,
                        eventHandler,
                        cancellationToken
                    );
                })
            .Invoke()
            .ConfigureAwait(false);
    }
    
    static void AppendHandlersIf<TEvent>(List<IEventHandler<TEvent>> list, IEventCollector collector) where TEvent : IEvent
    {
        var handlers = collector.GetHandlers<TEvent>();
        foreach (var handler in handlers)
        {
            if (!list.Contains(handler))
                list.Add(handler);
        }
    }
}