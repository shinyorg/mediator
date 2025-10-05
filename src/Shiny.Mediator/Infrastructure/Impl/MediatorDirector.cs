namespace Shiny.Mediator.Infrastructure.Impl;

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
    
    public IRequestExecutor GetRequestExecutor<TResult>(IRequest<TResult> request)
    {
        foreach (var exe in requestExecutors)
        {
            if (exe.CanHandle(request))
                return exe;
        }
        return this.requestExecutor;
    }

    public ICommandExecutor GetCommandExecutor(ICommand command)
    {
        foreach (var exe in commandExecutors)
        {
            if (exe.CanSend(command))
                return exe;
        }
        return this.commandExecutor;
    }

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

    public IEventExecutor GetEventExecutor<TEvent>() where TEvent : IEvent
    {
        foreach (var exe in eventExecutors)
        {
            if (exe.CanPublish(typeof(TEvent)))
                return exe;
        }

        return this.eventExecutor;
    }

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