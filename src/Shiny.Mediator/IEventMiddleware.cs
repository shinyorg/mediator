namespace Shiny.Mediator;


public delegate Task EventHandlerDelegate();
public interface IEventMiddleware<TEvent> where TEvent : IEvent
{
    Task Process(
        IEvent @event, 
        EventHandlerDelegate next, 
        IEventHandler<TEvent> eventHandler,
        CancellationToken cancellationToken
    );
}