using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


// TODO: validate 1 request handler per type (how?)
// TODO: validate all handlers (event or request) are scoped or singleton (how?)
public class Mediator(
    IServiceProvider services, 
    IEnumerable<IEventCollector> collectors
) : IMediator
{
    readonly SubscriptionEventCollector subscriptions = new();
    
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        // TODO: middleware execution should support contravariance
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        // TODO: pipelines
        await handlers
            .First()
            .Handle(request, cancellationToken)
            .ConfigureAwait(false);
    }
    
    
    public async Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResult));

        // TODO: middleware execution should support contravariance
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType).ToList();
        AssertRequestHandlers(handlers.Count, request);
        
        var handler = handlers.First();
        var handleMethod = handlerType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public)!;
        var resultTask = (Task<TResult>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        var result = await resultTask.ConfigureAwait(false);
        
        return result;
    }

    
    public async Task Publish<TEvent>(
        TEvent @event, 
        bool fireAndForget = true,
        bool executeInParallel = true,
        CancellationToken cancellationToken = default
    ) where TEvent : IEvent
    {
        // allow registered services to be transient/scoped/singleton
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ToList();
        //var globalHandlers = scope.ServiceProvider.GetServices<IEventHandler<IEvent>>().ToList();
        
        AppendHandlersIf(handlers, this.subscriptions);
        foreach (var collector in collectors)
            AppendHandlersIf(handlers, collector);

        if (handlers.Count == 0)
            return;

        Task executor = null!;
        if (executeInParallel)
        {
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

        // TODO: middleware
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
        foreach (var handler in handlers)
        {
            if (!list.Contains(handler))
                list.Add(handler);
        }
    }
    

    static void AssertRequestHandlers(int count, object request)
    {
        if (count == 0)
            throw new InvalidOperationException("No request handler found for " + request.GetType().FullName);

        if (count > 1)
            throw new InvalidOperationException("More than 1 request handlers found for " + request.GetType().FullName);
    }
}