using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


public class EventExecutor(
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
    
    
    async Task PublishCore<TEvent>(
        IMediatorContext context,
        TEvent @event,
        IEventHandler<TEvent> eventHandler, 
        ILogger logger,
        IEnumerable<IEventMiddleware<TEvent>> middlewares,
        CancellationToken cancellationToken
    ) where TEvent : IEvent
    {
        var handlerDelegate = new EventHandlerDelegate(async () =>
        {
            var postAction = context.Execution.OnHandlerExecute(context);
            
            logger.LogDebug(
                "Executing Event Handler {HandlerType}",
                eventHandler.GetType().FullName
            );
            await eventHandler
                .Handle(@event, context, cancellationToken)
                .ConfigureAwait(false);

            postAction.Invoke();
        });
           
        await middlewares
            .Reverse()
            .Aggregate(
                handlerDelegate, 
                (next, middleware) => () =>
                {
                    var postAction = context.Execution.OnMiddlewareExecute(context, middleware);
                    logger.LogDebug(
                        "Executing event middleware {MiddlewareType}",
                        middleware.GetType().FullName
                    );

                    return middleware
                        .Process(context, next, cancellationToken)
                        .ContinueWith(x => postAction.Invoke());
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