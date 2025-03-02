namespace Shiny.Mediator;


public delegate Task EventHandlerDelegate();
public interface IEventMiddleware<TEvent> where TEvent : IEvent
{
    Task Process(
        IMediatorContext context, 
        EventHandlerDelegate next,
        CancellationToken cancellationToken
    );
}