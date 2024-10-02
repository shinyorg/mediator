namespace Shiny.Mediator;


public class EventExecutionContext(
    CancellationToken cancellationToken
)
{
    public CancellationToken CancellationToken => cancellationToken;
    
    public Guid ExecutionId { get; }= Guid.NewGuid();
    
    readonly Dictionary<string, object> store = new();
    public IReadOnlyDictionary<string, object> Values => this.store.ToDictionary();
    public void Add(string key, object value) => this.store.Add(key, value);
}


public class EventExecutionContext<TEvent>(
    TEvent @event,
    IEventHandler<TEvent> eventHandler,
    CancellationToken cancellationToken
) : EventExecutionContext(cancellationToken) where TEvent : IEvent
{
    public TEvent Event => @event;
    public IEventHandler<TEvent> EventHandler => eventHandler;
}