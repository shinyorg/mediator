using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class DefaultEventPublisher(
    IServiceProvider services, 
    IEnumerable<IEventCollector> collectors
) : IEventPublisher
{
    readonly SubscriptionEventCollector subscriptions = new();

    
    public async Task<EventAggregatedExecutionContext<TEvent>> Publish<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default,
        bool executeInParallel = true
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

        var list = new List<EventExecutionContext<TEvent>>();
        var context = new EventAggregatedExecutionContext<TEvent>(list);
        
        if (handlers.Count == 0)
            return context;

        var logger = services.GetRequiredService<ILogger<TEvent>>();
        var middlewares = scope.ServiceProvider.GetServices<IEventMiddleware<TEvent>>().ToList();
        var tasks = handlers
            .Select(async x =>
            {
                var econtext = await Execute(@event, x, logger, middlewares, cancellationToken).ConfigureAwait(false);
                list.Add(econtext);
            })
            .ToList();

        if (executeInParallel)
        {
            await Task
                .WhenAll(tasks)
                .ConfigureAwait(false);
        }
        else
        {
            foreach (var task in tasks)
                await task.ConfigureAwait(false);
        }

        return context;
    }

    
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
    {
        var handler = new SubscriptionEventHandler<TEvent>(this.subscriptions);
        handler.OnHandle = action;
        return handler;
    }


    static async Task<EventExecutionContext<TEvent>> Execute<TEvent>(
        TEvent @event,
        IEventHandler<TEvent> eventHandler, 
        ILogger logger,
        IEnumerable<IEventMiddleware<TEvent>> middlewares,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var context = new EventExecutionContext<TEvent>(@event, eventHandler, cancellationToken);
        
        var handlerDelegate = new EventHandlerDelegate(() =>
        {
            logger.LogDebug(
                "Executing Event Handler {HandlerType}", 
                eventHandler.GetType().FullName
            );
            return eventHandler.Handle(context.Event, context.CancellationToken);
        });
        
        await middlewares
            .Reverse()
            .Aggregate(
                handlerDelegate, 
                (next, middleware) => () =>
                {
                    logger.LogDebug(
                        "Executing event middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );
                    
                    return middleware.Process(context, next);
                }
            )
            .Invoke()
            .ConfigureAwait(false);

        return context;
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