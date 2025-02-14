using Microsoft.UI.Xaml;
using Uno.Extensions.Navigation.UI;

namespace Shiny.Mediator.Infrastructure;


public class UnoEventCollector: IEventCollector, Uno.Extensions.Navigation.UI.IRequestHandler
{
    readonly List<FrameworkElement> events = new();
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        List<FrameworkElement> copy;
        lock (this.events)
            copy = this.events.ToList();
        
        var list = new List<IEventHandler<TEvent>>();
        foreach (var item in copy)
        {
            if (item.DataContext is IEventHandler<TEvent> vmHandler)
                list.Add(vmHandler);

            else if (item is IEventHandler<TEvent> viewHandler)
                list.Add(viewHandler);
        }

        return list;
    }

    public bool CanBind(FrameworkElement view) => true;
    public IRequestBinding? Bind(FrameworkElement view)
    {
        lock (this.events)
            this.events.Add(view);
        
        return new MediatorBinding(() =>
        {
            lock (this.events)
                this.events.Remove(view);
        });
    }
}

public class MediatorBinding(Action release) : IRequestBinding
{
    public void Unbind() => release.Invoke();
}