namespace Shiny.Mediator;

public class MauiEventCollector : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        // I need to make this crawl the tree, but really I don't need a ton of use-cases here
        var list = new List<IEventHandler<TEvent>>();
        var mainPage = Application.Current?.MainPage;
        if (mainPage == null)
            return list;
        
        if (mainPage is TabbedPage tabs)
        {
            foreach (var tab in tabs.Children)
            {
                if (tab is NavigationPage navPage)
                {
                    TryAppendEvents(navPage, list);
                }
                else
                {
                    TryAppendEvents(tab, list);
                }
            }
        }
        else if (mainPage is NavigationPage navPage)
        {
            TryAppendEvents(navPage, list);
        }
        else
        {
            TryAppendEvents(mainPage!, list);
        }

        return list;
    }


    static void TryAppendEvents<TEvent>(Page page, List<IEventHandler<TEvent>> list) where TEvent : IEvent
    {
        if (page is IEventHandler<TEvent> handler1)
            list.Add(handler1);
        
        if (page.BindingContext is IEventHandler<TEvent> handler2)
            list.Add(handler2);
    }

    
    static void TryAppendEvents<TEvent>(NavigationPage navPage, List<IEventHandler<TEvent>> list) where TEvent : IEvent
    {
        var navStack = navPage.Navigation?.NavigationStack;
        if (navStack != null)
        {
            foreach (var page in navStack)
            {
                TryAppendEvents(page, list);
            }
        }        
    }
}