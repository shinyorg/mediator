namespace Shiny.Mediator.Infrastructure.Impl;

/// <summary>
/// Default <see cref="IMediatorDirector"/> implementation. Walks each set of registered custom executors and
/// uses the first one that opts in via its <c>CanHandle</c>/<c>CanSend</c>/<c>CanPublish</c>/<c>CanRequest</c>
/// hook; otherwise falls back to the built-in local executor.
/// </summary>
public class MediatorDirector(
    IEnumerable<IEventCollector> eventCollectors,
    IEnumerable<IRequestExecutor> requestExecutors,
    IEnumerable<IStreamRequestExecutor> streamRequestExecutors,
    IEnumerable<IEventExecutor> eventExecutors,
    IEnumerable<ICommandExecutor> commandExecutors
) : IMediatorDirector
{
    readonly LocalRequestExecutor requestExecutor = new();
    readonly LocalCommandExecutor commandExecutor = new();
    readonly LocalEventExecutor eventExecutor = new(eventCollectors);
    readonly LocalStreamRequestExecutor streamRequestExecutor = new();

    /// <inheritdoc/>
    public IRequestExecutor GetRequestExecutor<TResult>(IRequest<TResult> request)
    {
        foreach (var exe in requestExecutors)
        {
            if (exe.CanHandle(request))
                return exe;
        }
        return this.requestExecutor;
    }

    /// <inheritdoc/>
    public ICommandExecutor GetCommandExecutor(ICommand command)
    {
        foreach (var exe in commandExecutors)
        {
            if (exe.CanSend(command))
                return exe;
        }
        return this.commandExecutor;
    }

    /// <inheritdoc/>
    public IEventExecutor GetEventExecutor(IEvent @event)
    {
        var eventType = @event.GetType();
        foreach (var exe in eventExecutors)
        {
            if (exe.CanPublish(eventType))
                return exe;
        }

        return this.eventExecutor;
    }

    /// <inheritdoc/>
    public IEventExecutor GetEventExecutor<TEvent>() where TEvent : IEvent
    {
        foreach (var exe in eventExecutors)
        {
            if (exe.CanPublish(typeof(TEvent)))
                return exe;
        }

        return this.eventExecutor;
    }

    /// <inheritdoc/>
    public IStreamRequestExecutor GetStreamRequestExecutor<TResult>(IStreamRequest<TResult> request)
    {
        foreach (var exe in streamRequestExecutors)
        {
            if (exe.CanRequest(request))
                return exe;
        }

        return this.streamRequestExecutor;
    }
}