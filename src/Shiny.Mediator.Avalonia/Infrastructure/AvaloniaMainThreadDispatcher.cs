using Avalonia.Threading;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Avalonia <see cref="IMainThreadDispatcher"/> backed by <see cref="Dispatcher.UIThread"/>.
/// </summary>
public class AvaloniaMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc/>
    public bool IsMainThread => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc/>
    public void BeginInvokeOnMainThread(Action action) => Dispatcher.UIThread.Post(action);
}
