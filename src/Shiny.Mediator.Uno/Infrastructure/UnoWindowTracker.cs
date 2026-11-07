using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Tracks the Uno Platform <see cref="Window"/>s that Shiny Mediator works against. The
/// <see cref="UnoEventCollector"/> walks their visual trees, the <see cref="AlertDialogService"/>
/// shows dialogs on them, and the <see cref="UnoMainThreadDispatcher"/> uses their dispatcher.
/// Windows are added by <c>UseUno(window)</c> or <c>IApplicationBuilder.AddShinyMediator</c>; add any
/// secondary window yourself with <see cref="Add"/>. A window is removed when it closes.
/// </summary>
public class UnoWindowTracker
{
    readonly Lock sync = new();
    readonly List<Window> windows = new();
    DispatcherQueue? fallbackDispatcher = DispatcherQueue.GetForCurrentThread();


    /// <summary>
    /// Creates a tracker holding the windows registered through <c>UseUno(window)</c>.
    /// </summary>
    public UnoWindowTracker(IEnumerable<UnoWindowRegistration>? registrations = null)
    {
        if (registrations == null)
            return;

        foreach (var registration in registrations)
            this.Add(registration.Window);
    }


    /// <summary>
    /// Starts tracking <paramref name="window"/>. Adding the same window twice is a no-op.
    /// </summary>
    public void Add(Window window)
    {
        lock (this.sync)
        {
            if (this.windows.Contains(window))
                return;

            this.windows.Add(window);
        }
        this.fallbackDispatcher ??= window.DispatcherQueue;
        window.Closed += (_, _) => this.Remove(window);
    }


    /// <summary>
    /// Stops tracking <paramref name="window"/>.
    /// </summary>
    public void Remove(Window window)
    {
        lock (this.sync)
            this.windows.Remove(window);
    }


    /// <summary>
    /// A snapshot of the tracked windows, in the order they were added.
    /// </summary>
    public IReadOnlyList<Window> Windows
    {
        get
        {
            lock (this.sync)
                return this.windows.ToArray();
        }
    }


    /// <summary>
    /// The first tracked window, or <c>null</c> when no window is tracked.
    /// </summary>
    public Window? MainWindow
    {
        get
        {
            lock (this.sync)
                return this.windows.FirstOrDefault();
        }
    }


    /// <summary>
    /// The UI thread dispatcher: the main window's <see cref="DispatcherQueue"/>, falling back to
    /// the dispatcher of the thread that created the tracker (the UI thread during app startup).
    /// </summary>
    public DispatcherQueue? Dispatcher => this.MainWindow?.DispatcherQueue ?? this.fallbackDispatcher;
}


/// <summary>
/// A window handed to <c>UseUno(window)</c>; picked up by <see cref="UnoWindowTracker"/> when it is created.
/// </summary>
/// <param name="Window">The window to track.</param>
public sealed record UnoWindowRegistration(Window Window);
