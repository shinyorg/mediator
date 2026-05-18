namespace Shiny.Mediator;


/// <summary>
/// Entry point for dispatching commands, requests, and events through the mediator pipeline.
/// Each call produces an <see cref="IMediatorContext"/> describing the execution that occurred.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a command through the configured command middleware pipeline to its registered handler.
    /// </summary>
    /// <param name="request">The command to dispatch.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure">Optional callback to configure the <see cref="IMediatorContext"/> before execution (e.g. headers, bypass flags).</param>
    /// <returns>The completed context describing the command execution.</returns>
    Task<IMediatorContext> Send<TCommand>(
        TCommand request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand;


    /// <summary>
    /// Sends a request through the request pipeline and returns both the resulting context and the handler's typed result.
    /// </summary>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure">Optional callback to configure the <see cref="IMediatorContext"/> before execution.</param>
    Task<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    );


    /// <summary>
    /// Dispatches a stream request and returns an async sequence of (context, result) tuples produced by the handler pipeline.
    /// </summary>
    /// <param name="request">The stream request to dispatch.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure">Optional callback to configure the <see cref="IMediatorContext"/> before execution.</param>
    IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    );


    /// <summary>
    /// Publishes an event to every registered handler for <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="executeInParallel">When true, handlers run concurrently; when false, they run sequentially in registration order.</param>
    /// <param name="cancellationToken"></param>
    /// <param name="configure">Optional callback to configure the <see cref="IMediatorContext"/> before execution.</param>
    Task<IMediatorContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;


    /// <summary>
    /// Fires an event without awaiting handlers. A fresh service scope is created for the background execution
    /// so the caller's scope can be disposed safely while handlers continue running.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="executeInParallel">When true, handlers run concurrently; when false, they run sequentially.</param>
    /// <param name="configure">Optional callback to configure the <see cref="IMediatorContext"/> before execution.</param>
    void PublishToBackground<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent;

    /// <summary>
    /// Subscribes to events of <typeparamref name="TEvent"/> outside the DI container.
    /// The returned <see cref="IDisposable"/> removes the subscription when disposed.
    /// </summary>
    /// <param name="action">Callback invoked for each published event.</param>
    /// <returns>A disposable handle that unsubscribes when disposed.</returns>
    IDisposable Subscribe<TEvent>(
        Func<TEvent, IMediatorContext, CancellationToken, Task> action
    ) where TEvent : IEvent;
}
