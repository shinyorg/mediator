using Shiny.Mediator.Impl;

namespace Shiny.Mediator;

public class MediatorEventHandler<TEvent> : IDisposable, IEventHandler<TEvent> where TEvent : IEvent
{
    readonly EventCollector collector;
    
    
    public MediatorEventHandler(EventCollector collector)
    {
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