namespace Shiny.Mediator.Infrastructure;

public class SentryEventMiddleware<TEvent>(Func<IHub> getHub) : IEventMiddleware<TEvent> where TEvent : IEvent
{
    public Task Process(EventContext<TEvent> context, EventHandlerDelegate next, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}