namespace Shiny.Mediator.Infrastructure;

public interface IMediatorDirector
{
    /// <summary>
    /// Get the request executor for the given request
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IRequestExecutor GetRequestExecutor<TResult>(IRequest<TResult> request);
    
    /// <summary>
    /// Get the command executor for the given command
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    ICommandExecutor GetCommandExecutor(ICommand command);
    
    /// <summary>
    /// Get the event executor for the given event
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    IEventExecutor GetEventExecutor(IEvent @event);
    
    /// <summary>
    /// Gets the event executor for the given event type
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IEventExecutor GetEventExecutor<TEvent>() where TEvent : IEvent;
    
    /// <summary>
    /// Get the stream request executor for the given request
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    IStreamRequestExecutor GetStreamRequestExecutor<TResult>(IStreamRequest<TResult> request);
}