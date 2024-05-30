using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


// TODO: validate 1 request handler per type (how?)
// TODO: validate all handlers (event or request) are scoped or singleton (how?)
public class Mediator(
    IServiceProvider services, 
    IEventCollector? collector = null
) : IMediator
{
    readonly SubscriptionEventCollector subscriptions = new();
    
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        var handlers = services.GetServices<IRequestHandler<TRequest>>().ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        // TODO: pipelines
        await handlers.First().Handle(request, cancellationToken).ConfigureAwait(false);
    }
    
    
    public async Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));
        var handlers = services.GetServices(handlerType).ToList();
        AssertRequestHandlers(handlers.Count, request);

        var handler = handlers.First();
        var handleMethod = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public)!;
        var resultTask = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        var result = await resultTask.ConfigureAwait(false);

        // TODO: pipelines
        return result;
    }

    
    public async Task Publish<TEvent>(
        TEvent @event, 
        bool fireAndForget = true,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default
    ) where TEvent : IEvent
    {
        var handlers = services.GetServices<IEventHandler<TEvent>>().ToList();
        AppendHandlersIf(handlers, this.subscriptions);
        if (collector != null)
            AppendHandlersIf(handlers, collector);

        if (handlers.Count == 0)
            return;
        
        Task executor = null!;
        if (executeInParallel)
        {
            // TODO: pipelines? error management?
            executor = Task.WhenAll(handlers.Select(x => x.Handle(@event, cancellationToken)).ToList());
        }
        else
        {
            executor = Task.Run(async () =>
            {
                foreach (var handler in handlers)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // TODO: pipelines? error management?
                        await handler
                            .Handle(@event, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            });
        }
        
        // TODO: pipelines
        if (fireAndForget)
        {
            this.FireAndForget(executor);
        }
        else
        {
            await executor.ConfigureAwait(false);
        }
    }

    
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
    {
        var handler = new SubscriptionEventHandler<TEvent>(this.subscriptions);
        handler.OnHandle = action;
        return handler;
    }


    async void FireAndForget(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: this should call the error pipeline
        }
    }

    
    static void AppendHandlersIf<TEvent>(List<IEventHandler<TEvent>> list, IEventCollector collector) where TEvent : IEvent
    {
        var handlers = collector.GetHandlers<TEvent>();
        if (handlers.Count > 0)
            list.AddRange(handlers);
    }
    

    static void AssertRequestHandlers(int count, object request)
    {
        if (count == 0)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        if (count > 1)
            throw new InvalidOperationException("More than 1 request handlers found for " + request.GetType().FullName);
    }
}