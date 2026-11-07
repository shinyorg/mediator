using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno Platform <see cref="IEventCollector"/>. When an event is published, it walks the visual tree of
/// every tracked <see cref="Window"/> (plus open popups) on the UI thread and returns each
/// <see cref="FrameworkElement"/> or <c>DataContext</c> that implements
/// <see cref="IEventHandler{TEvent}"/>. A view model shared by many elements is returned once.
/// </summary>
public class UnoEventCollector(
    UnoWindowTracker windowTracker,
    ILogger<UnoEventCollector>? logger = null
) : IEventCollector
{
    /// <summary>
    /// How long a publish from a background thread waits for the UI thread to collect handlers before
    /// giving up and skipping view handlers. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan UiThreadTimeout { get; set; } = TimeSpan.FromSeconds(5);


    /// <inheritdoc/>
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        var windows = windowTracker.Windows;
        if (windows.Count == 0)
            return [];

        var dispatcher = windowTracker.Dispatcher;
        if (dispatcher == null || dispatcher.HasThreadAccess)
            return Collect<TEvent>(windows);

        // the visual tree and DataContext can only be read on the UI thread
        IReadOnlyList<IEventHandler<TEvent>> result = [];
        using var done = new ManualResetEventSlim(false);
        var queued = dispatcher.TryEnqueue(() =>
        {
            try
            {
                result = Collect<TEvent>(windows);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to collect Uno view event handlers for {EventType}", typeof(TEvent).FullName);
            }
            finally
            {
                done.Set();
            }
        });

        if (!queued || !done.Wait(this.UiThreadTimeout))
        {
            logger?.LogWarning("Timed out collecting Uno view event handlers for {EventType} - is the UI thread blocked?", typeof(TEvent).FullName);
            return [];
        }
        return result;
    }


    IReadOnlyList<IEventHandler<TEvent>> Collect<TEvent>(IReadOnlyList<Window> windows) where TEvent : IEvent
    {
        var found = new HashSet<IEventHandler<TEvent>>(ReferenceEqualityComparer.Instance);
        var list = new List<IEventHandler<TEvent>>();

        foreach (var window in windows)
        {
            if (window.Content is not { } root)
                continue;

            Walk(root, null, found, list);

            if (root.XamlRoot != null)
            {
                foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot))
                {
                    if (popup.Child != null)
                        Walk(popup.Child, null, found, list);
                }
            }
        }
        logger?.LogDebug("Found {Count} Uno views/view models for event {EventType}", list.Count, typeof(TEvent).FullName);
        return list;
    }


    static void Walk<TEvent>(
        DependencyObject element,
        object? parentDataContext,
        HashSet<IEventHandler<TEvent>> found,
        List<IEventHandler<TEvent>> list
    ) where TEvent : IEvent
    {
        var dataContext = parentDataContext;
        if (element is FrameworkElement fe)
        {
            if (fe is IEventHandler<TEvent> viewHandler && found.Add(viewHandler))
                list.Add(viewHandler);

            dataContext = fe.DataContext;

            // DataContext is inherited - only inspect it where it changes
            if (!ReferenceEquals(dataContext, parentDataContext) && 
                dataContext is IEventHandler<TEvent> vmHandler && 
                found.Add(vmHandler))
            {
                list.Add(vmHandler);
            }
        }

        var count = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < count; i++)
            Walk(VisualTreeHelper.GetChild(element, i), dataContext, found, list);
    }
}
