using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator
{
    readonly SubscriptionEventCollector subscriptions = new();

    public virtual async Task<EventAggregatedContext<TEvent>> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        params IEnumerable<(string Key, object Value)> headers
    ) where TEvent : IEvent
    {
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToList();
        
        AppendHandlersIf(handlers, this.subscriptions);
        foreach (var collector in collectors)
            AppendHandlersIf(handlers, collector);

        var list = new List<EventContext<TEvent>>();
        var context = new EventAggregatedContext<TEvent>(list);
           
        if (handlers.Count == 0)
            return context;

        var logger = services.GetRequiredService<ILogger<TEvent>>();
        var middlewares = scope.ServiceProvider.GetServices<IEventMiddleware<TEvent>>().ToList();
        var tasks = handlers
            .Select(async handler =>
            {
                var econtext = await this
                    .PublishCore(
                        @event, 
                        handler, 
                        headers,
                        logger, 
                        middlewares,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
                
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

    public IDisposable Subscribe<TEvent>(Func<TEvent, EventContext<TEvent>, CancellationToken, Task> action) where TEvent : IEvent
    {
        var handler = new SubscriptionEventHandler<TEvent>(this.subscriptions);
        handler.OnHandle = action;
        return handler;
    }
    
    
    async Task<EventContext<TEvent>> PublishCore<TEvent>(
        TEvent @event,
        IEventHandler<TEvent> eventHandler, 
        IEnumerable<(string Key, object Value)> headers,
        ILogger logger,
        IEnumerable<IEventMiddleware<TEvent>> middlewares,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var context = new EventContext<TEvent>(@event, eventHandler);
        context.PopulateHeaders(headers);
        
        var handlerDelegate = new EventHandlerDelegate(() =>
        {
            logger.LogDebug(
                "Executing Event Handler {HandlerType}", 
                eventHandler.GetType().FullName
            );
            return eventHandler.Handle(context.Event, context, cancellationToken);
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
                       
                    return middleware.Process(context, next, cancellationToken);
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