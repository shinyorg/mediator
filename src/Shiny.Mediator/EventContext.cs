namespace Shiny.Mediator;


public class EventContext
{
    public Guid EventId { get; }= Guid.NewGuid();
    
    readonly Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
}


public class EventContext<TEvent>(
    TEvent @event,
    IEventHandler<TEvent> eventHandler,
    CancellationToken cancellationToken
) : EventContext where TEvent : IEvent
{
    public TEvent Event => @event;
    public IEventHandler<TEvent> EventHandler => eventHandler;
}