using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Avalonia <see cref="IEventCollector"/>. When an event is published, it walks the logical tree of
/// every open top level (all windows of a desktop app, or the main view of a mobile/browser app)
/// on the UI thread and returns each control or <see cref="StyledElement.DataContext"/> that
/// implements <see cref="IEventHandler{TEvent}"/>. A view model shared by many controls is returned once.
/// </summary>
public class AvaloniaEventCollector(ILogger<AvaloniaEventCollector>? logger = null) : IEventCollector
{
    /// <summary>
    /// How long a publish from a background thread waits for the UI thread to collect handlers before
    /// giving up and skipping view handlers. Defaults to 5 seconds.
    /// </summary>
    public TimeSpan UiThreadTimeout { get; set; } = TimeSpan.FromSeconds(5);


    /// <inheritdoc/>
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        if (Application.Current == null)
            return [];

        if (Dispatcher.UIThread.CheckAccess())
            return this.Collect<TEvent>();

        // controls and DataContext can only be read on the UI thread
        try
        {
            return Dispatcher.UIThread.Invoke(
                this.Collect<TEvent>,
                DispatcherPriority.Send,
                CancellationToken.None,
                this.UiThreadTimeout
            ) ?? [];
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to collect Avalonia view event handlers for {EventType} - is the UI thread blocked?", typeof(TEvent).FullName);
            return [];
        }
    }


    /// <summary>
    /// The roots to search for event handlers. By default, the windows of an
    /// <see cref="IClassicDesktopStyleApplicationLifetime"/> or the main view of an
    /// <see cref="ISingleViewApplicationLifetime"/>. Called on the UI thread.
    /// </summary>
    protected virtual IEnumerable<ILogical> GetRoots()
    {
        switch (Application.Current?.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                return desktop.Windows;

            case ISingleViewApplicationLifetime { MainView: { } mainView }:
                return [mainView];

            default:
                return [];
        }
    }


    IReadOnlyList<IEventHandler<TEvent>> Collect<TEvent>() where TEvent : IEvent
    {
        var found = new HashSet<IEventHandler<TEvent>>(ReferenceEqualityComparer.Instance);
        var list = new List<IEventHandler<TEvent>>();

        foreach (var root in this.GetRoots())
        {
            foreach (var element in root.GetSelfAndLogicalDescendants())
            {
                if (element is IEventHandler<TEvent> viewHandler && found.Add(viewHandler))
                    list.Add(viewHandler);

                if (element is StyledElement { DataContext: IEventHandler<TEvent> vmHandler } && found.Add(vmHandler))
                    list.Add(vmHandler);
            }
        }
        logger?.LogDebug("Found {Count} Avalonia views/view models for event {EventType}", list.Count, typeof(TEvent).FullName);
        return list;
    }
}
