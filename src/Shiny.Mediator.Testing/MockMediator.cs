using Shiny.Mediator.Testing;

namespace Shiny.Mediator;

public class MockMediator(
    Func<object, IMediatorContext, object>? onRequest = null,
    Func<object, IMediatorContext, IAsyncEnumerable<object>>? onStreamRequest = null,
    Action<object, IMediatorContext>? onCommand = null,
    Action<object, bool, IMediatorContext>? onPublish = null
) : IMediator
{
    
    public Task<IMediatorContext> Send<TCommand>(TCommand command, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null) where TCommand : ICommand
    {
        var ctx = new MockMediatorContext(command);
        configure?.Invoke(ctx);
        onCommand?.Invoke(ctx, ctx);
        
        return Task.FromResult<IMediatorContext>(ctx);
    }

    public Task<(IMediatorContext Context, TResult Result)> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
    {
        if (onRequest == null)
            throw new NotImplementedException("onRequest callback must be provided for MockMediator to handle requests");

        var ctx = new MockMediatorContext(request);
        configure?.Invoke(ctx);
        var response = onRequest.Invoke(request, ctx);
        if (response is TResult result)
            return Task.FromResult(((IMediatorContext)ctx, result));
        
        throw new InvalidOperationException($"Expected response of type {typeof(TResult).Name}, but got {response?.GetType().Name ?? "null"}");
    }

    public async IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IStreamRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        if (onStreamRequest == null)
            throw new NotImplementedException("onStreamRequest callback must be provided for MockMediator to handle stream requests");

        var ctx = new MockMediatorContext(request);
        configure?.Invoke(ctx);
        
        var response = onStreamRequest.Invoke(request, ctx);
        await foreach (var item in response)
            yield return (ctx, (TResult)item);
    }

    public Task<IMediatorContext> Publish<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default, 
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        var ctx = new MockMediatorContext(@event);
        configure?.Invoke(ctx);
        onPublish?.Invoke(@event, executeInParallel, ctx);

        return Task.FromResult<IMediatorContext>(ctx);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action)
        where TEvent : IEvent => throw new NotImplementedException();

    // readonly List<(Type EventType, Action<object, IMediatorContext> Handler)> subscriptions = new();
    //
    // public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action)
    //     where TEvent : IEvent
    // {
    //     var ptr = (object e, IMediatorContext ctx) =>
    //     {
    //         action.Invoke((TEvent)e, ctx, CancellationToken.None);
    //     });
    //     this.subscriptions.Add((typeof(TEvent), ptr);
    //     return new Disposer(() => this.subscriptions.Remove);
    // }
    
}

// file class Disposer(Action disposeAction) : IDisposable
// {
//     public void Dispose() => disposeAction();
// }