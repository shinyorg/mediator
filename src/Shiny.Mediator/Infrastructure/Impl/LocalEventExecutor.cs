using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class LocalEventExecutor(
    IEnumerable<IEventCollector> collectors
) : IEventExecutor
{
    readonly SubscriptionEventCollector subscriptions = new();

    public async Task Publish<TEvent>(
        IMediatorContext context,
        TEvent @event,
        bool executeInParallel,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var services = context.ServiceScope.ServiceProvider;
        var handlers = services.GetServices<IEventHandler<TEvent>>().ToList();
        
        AppendHandlersIf(handlers, this.subscriptions);
        foreach (var collector in collectors)
            AppendHandlersIf(handlers, collector);

        if (handlers.Count == 0)
            return;
        
        var logger = services.GetRequiredService<ILogger<TEvent>>();
        var bypass = context.BypassMiddlewareEnabled;
        var middlewares = bypass ? [] : services.GetServices<IEventMiddleware<TEvent>>();
        
        var tasks = handlers
            .Select(async handler =>
            {
                var child = context.CreateChild(null);
                child.MessageHandler = handler;
                
                await this
                    .PublishCore(
                        child,
                        @event, 
                        handler, 
                        logger, 
                        middlewares,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
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
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action) where TEvent : IEvent
    {
        var handler = new SubscriptionEventHandler<TEvent>(this.subscriptions);
        handler.OnHandle = action;
        return handler;
    }

    public bool CanPublish<TEvent>(TEvent @event) where TEvent : IEvent => true;
    public bool CanPublish(Type eventType) => true;


    async Task PublishCore<TEvent>(
        IMediatorContext context,
        TEvent @event,
        IEventHandler<TEvent> eventHandler, 
        ILogger logger,
        IEnumerable<IEventMiddleware<TEvent>> middlewares,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var handlerDelegate = new EventHandlerDelegate(() =>
        {
            using (var handlerActivity = context.StartActivity("Handler"))
            {
                logger.LogDebug(
                    "Executing Event Handler {HandlerType}",
                    eventHandler.GetType().FullName
                );
                return eventHandler.Handle(@event, context, cancellationToken);
            }
        });
           
        await middlewares
            .Reverse()
            .Aggregate(
                handlerDelegate, 
                (next, middleware) => () =>
                {
                    using (var midActivity = context.StartActivity("Middleware"))
                    {
                        logger.LogDebug(
                            "Executing event middleware {MiddlewareType}",
                            middleware.GetType().FullName
                        );

                        return middleware.Process(context, next, cancellationToken);
                    }
                }
            )
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