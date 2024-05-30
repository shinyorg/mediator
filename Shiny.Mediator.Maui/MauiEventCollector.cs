namespace Shiny.Mediator;

public class MauiEventCollector : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        var list = new List<IEventHandler<TEvent>>();
        var proxy = Application.Current?.NavigationProxy;
        if (proxy != null)
        {
            foreach (var page in proxy.NavigationStack)
            {
                // if (page is TabbedPage tabs)
                // {
                //     foreach (var tab in tabs.Children)
                //     {
                //         tab.NavigationProxy
                //     }
                // }
                // if (page is NavigationPage nav)
                // {
                //     nav.NavigationProxy
                // }
                    
                if (page is IEventHandler<TEvent> handler1)
                    list.Add(handler1);
                
                if (page.BindingContext is IEventHandler<TEvent> handler2)
                    list.Add(handler2);
            }
        }
        return list;
    }
}