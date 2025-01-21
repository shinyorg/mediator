namespace Shiny.Mediator;


public delegate Task EventHandlerDelegate();
public interface IEventMiddleware<TEvent> where TEvent : IEvent
{
    Task Process(
        EventContext<TEvent> context, 
        EventHandlerDelegate next
    );
}