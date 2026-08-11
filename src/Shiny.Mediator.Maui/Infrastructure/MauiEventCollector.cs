using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// MAUI <see cref="IEventCollector"/> that tracks every <see cref="Page"/> attached to the
/// application's visual tree (via <c>DescendantAdded</c>/<c>DescendantRemoved</c>), then resolves
/// <see cref="IEventHandler{TEvent}"/> implementations from each page and its binding context
/// when events are dispatched.
/// </summary>
public class MauiEventCollector(ILogger<MauiEventCollector>? logger = null) : IMauiInitializeService, IEventCollector
{
    readonly Lock sync = new();
    readonly List<Page> trackingPages = new();

    /// <inheritdoc/>
    public void Initialize(IServiceProvider services)
    {
        var application = services.GetRequiredService<IApplication>() as Application;
        if (application == null)
        {
            logger?.LogWarning("Application was not detected properly and cannot be wired");
            return;
        }
        application.DescendantAdded += (_, args) =>
        {
            if (args.Element is Page page)
            {
                lock (this.sync)
                    this.trackingPages.Add(page);

                logger?.LogDebug("Tracking {count} pages", this.trackingPages.Count);
            }
        };
        application.DescendantRemoved += (_, args) =>
        {
            if (args.Element is Page page)
            {
                lock (this.sync)
                    this.trackingPages.Remove(page);

                logger?.LogDebug("Tracking {count} pages", this.trackingPages.Count);
            }
        };
    }


    /// <inheritdoc/>
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        logger?.LogDebug("Collecting MAUI Pages/binding contexts for Event Handler Type: {type}", typeof(TEvent).FullName);

        var list = new List<IEventHandler<TEvent>>();
        lock (this.sync)
        {
            foreach (var page in this.trackingPages)
            {
                if (page is IEventHandler<TEvent> handler1)
                    list.Add(handler1);

                if (page.BindingContext is IEventHandler<TEvent> handler2)
                    list.Add(handler2);
            }
        }
        logger?.LogDebug(
            "Found {count} MAUI pages/binding contexts for Event Hander Type: {type}",
            this.trackingPages.Count,
            typeof(TEvent).FullName
        );
        return list;
    }
}