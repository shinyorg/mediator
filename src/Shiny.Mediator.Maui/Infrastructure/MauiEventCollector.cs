using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


public class MauiEventCollector(IApplication app, ILogger<MauiEventCollector> logger) : IMauiInitializeService, IEventCollector
{
    readonly List<Page> trackingPages = new();
    
    public void Initialize(IServiceProvider services)
    {
        var application = app as Application;
        if (application == null)
        {
            logger.LogWarning("Application was not detected properly and cannot be wired");
            return;
        }
        application.DescendantAdded += (_, args) =>
        {
            if (args.Element is Page page)
            {
                this.trackingPages.Add(page);
                logger.LogDebug("Tracking {count} pages", this.trackingPages.Count);
            }
        };
        application.DescendantRemoved += (_, args) =>
        {
            if (args.Element is Page page)
            {
                this.trackingPages.Remove(page);
                logger.LogDebug("Tracking {count} pages", this.trackingPages.Count);
            }
        };
    }

    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        logger.LogDebug("Collecting MAUI Pages/binding contexts for Event Handler Type: {type}", typeof(TEvent).FullName);
        
        var list = new List<IEventHandler<TEvent>>();
        foreach (var page in this.trackingPages)
        {
            if (page is IEventHandler<TEvent> handler1)
                list.Add(handler1);
            
            if (page.BindingContext is IEventHandler<TEvent> handler2)
                list.Add(handler2);
        }
        logger.LogDebug(
            "Found {count} MAUI pages/binding contexts for Event Hander Type: {type}",
            this.trackingPages.Count,
            typeof(TEvent).FullName
        );
        return list;
    }
}