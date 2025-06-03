namespace Shiny.Mediator;

public class MockMediator : IMediator
{
    public Task<IMediatorContext> Send<TCommand>(TCommand request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null) where TCommand : ICommand
    {
        throw new NotImplementedException();
    }

    public Task<(IMediatorContext Context, TResult Result)> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default, Action<IMediatorContext>? configure = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null)
    {
        throw new NotImplementedException();
    }

    public Task<IMediatorContext> Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default, bool executeInParallel = true,
        Action<IMediatorContext>? configure = null) where TEvent : IEvent
    {
        throw new NotImplementedException();
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action) where TEvent : IEvent
    {
        throw new NotImplementedException();
    }
}