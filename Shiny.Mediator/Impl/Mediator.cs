using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Impl;


public class Mediator(
    IServiceProvider services, 
    EventCollector collector
) : IMediator
{
    public async Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : IRequest
    {
        var handler = this.Resolve<IRequestHandler<TCommand>>();
        await handler.Handle(command, cancellationToken).ConfigureAwait(false);
    }
    

    public async Task<TResult> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : IRequest<TResult>
    {
        var handler = this.Resolve<IRequestHandler<TCommand, TResult>>();
        var result = await handler.Handle(command, cancellationToken).ConfigureAwait(false);
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
        var liveHandlers = collector.GetHandlers<TEvent>().ToArray();
        handlers.AddRange(liveHandlers);
        
        if (!handlers.Any())
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
                        await handler
                            .Handle(@event, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            });
        }
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
        var handler = new MediatorEventHandler<TEvent>(collector);
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
            // TODO: this should 
        }
    }

    T Resolve<T>()
    {
        var serviceRaw = services.GetService(typeof(T));
        if (serviceRaw == null)
            throw new InvalidOperationException("");

        return (T)serviceRaw;
    }
}