namespace Shiny.Mediator.Infrastructure;


public class MauiEventCollector : IEventCollector
{
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        // I need to make this crawl the tree, but really I don't need a ton of use-cases here
        if (Application.Current == null)
            return Array.Empty<IEventHandler<TEvent>>();
        
        if (Application.Current.Windows.Count == 0)
            return Array.Empty<IEventHandler<TEvent>>();
        
        var list = new List<IEventHandler<TEvent>>();
        foreach (var window in Application.Current.Windows)
        {
            if (window.Page != null)
                VisitPage(window.Page, list);
        }
        return list;
    }


    static void VisitPage<TEvent>(Page page, List<IEventHandler<TEvent>> list) where TEvent : IEvent
    {
        if (page is TabbedPage tabs)
        {
            foreach (var tab in tabs.Children)
            {
                TryNavPage(tab, list);
            }
        }
        else if (page is FlyoutPage flyout)
        {
            TryNavPage(flyout.Flyout, list);
            TryNavPage(flyout.Detail, list); // could be a tabs page
        }
        else
        {
            TryNavPage(page, list);
        }
    }
    

    static void TryNavPage<TEvent>(Page page, List<IEventHandler<TEvent>> list) where TEvent : IEvent
    {
        if (page is NavigationPage navPage)
        {
            TryAppendEvents(navPage, list);
        }
        else if (page is Shell shell)
        {
            TryAppendEvents(shell, list);
        }
        else
        {
            TryAppendEvents(page, list);
        }
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
    

    static void TryAppendEvents<TEvent>(Shell shell, List<IEventHandler<TEvent>> list) where TEvent : IEvent
    {
        var navStack = shell.Navigation?.NavigationStack;
        if (navStack != null)
        {
            foreach (var page in navStack)
            {
                if (page != null)
                {
                    TryAppendEvents(page, list);
                }
            }
        }
    }
}