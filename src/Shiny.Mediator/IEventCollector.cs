namespace Shiny.Mediator;

public interface IEventCollector
{
    /// <summary>
    /// Collects IEventHandler types that may be out-of-proc and not part of dependency injection
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent;
}