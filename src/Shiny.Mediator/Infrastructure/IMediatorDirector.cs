namespace Shiny.Mediator.Infrastructure;

/// <summary>
/// Selects the appropriate executor for each message dispatched through the mediator. Custom executors
/// are queried first via their <c>CanHandle</c>/<c>CanSend</c>/<c>CanPublish</c>/<c>CanRequest</c> hooks;
/// the built-in local executor is the fallback.
/// </summary>
public interface IMediatorDirector
{
    /// <summary>
    /// Returns the <see cref="IRequestExecutor"/> that will handle <paramref name="request"/>.
    /// </summary>
    IRequestExecutor GetRequestExecutor<TResult>(IRequest<TResult> request);

    /// <summary>
    /// Returns the <see cref="ICommandExecutor"/> that will handle <paramref name="command"/>.
    /// </summary>
    ICommandExecutor GetCommandExecutor(ICommand command);

    /// <summary>
    /// Returns the <see cref="IEventExecutor"/> that will publish <paramref name="event"/>.
    /// </summary>
    IEventExecutor GetEventExecutor(IEvent @event);

    /// <summary>
    /// Returns the <see cref="IEventExecutor"/> for events of <typeparamref name="TEvent"/>, regardless of instance.
    /// Used when no event instance is available (e.g. for <c>Subscribe</c>).
    /// </summary>
    IEventExecutor GetEventExecutor<TEvent>() where TEvent : IEvent;

    /// <summary>
    /// Returns the <see cref="IStreamRequestExecutor"/> that will handle <paramref name="request"/>.
    /// </summary>
    IStreamRequestExecutor GetStreamRequestExecutor<TResult>(IStreamRequest<TResult> request);
}
