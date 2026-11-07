namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno Platform <see cref="IMainThreadDispatcher"/> backed by the tracked window's
/// <see cref="Microsoft.UI.Dispatching.DispatcherQueue"/>.
/// </summary>
public class UnoMainThreadDispatcher(UnoWindowTracker windowTracker) : IMainThreadDispatcher
{
    /// <inheritdoc/>
    public bool IsMainThread => windowTracker.Dispatcher?.HasThreadAccess ?? false;


    /// <inheritdoc/>
    public void BeginInvokeOnMainThread(Action action)
    {
        var dispatcher = windowTracker.Dispatcher 
            ?? throw new InvalidOperationException("No UI dispatcher is available. Pass your Window to UseUno(window) or call AddShinyMediator on the Uno IApplicationBuilder");

        if (!dispatcher.TryEnqueue(() => action()))
            throw new InvalidOperationException("The UI dispatcher refused the work item; it is likely shutting down");
    }
}
