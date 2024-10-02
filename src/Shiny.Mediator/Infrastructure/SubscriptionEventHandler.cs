namespace Shiny.Mediator.Infrastructure;


public class SubscriptionEventHandler<TEvent> : IDisposable, IEventHandler<TEvent> where TEvent : IEvent
{
    readonly SubscriptionEventCollector collector;
    
    
    public SubscriptionEventHandler(SubscriptionEventCollector collector)
    {
        this.collector = collector;
        this.collector.Add(this);    
    }
    
    
    public Func<TEvent, CancellationToken, Task>? OnHandle { get; set; }
    
    public Task Handle(TEvent @event, CancellationToken cancellationToken)
    {
        if (this.OnHandle == null)
            throw new InvalidOperationException("MediatorEventHandler.OnHandle is not set");
        
        return this.OnHandle.Invoke(@event, cancellationToken);
    }


    public void Dispose() => this.collector.Remove(this);
}