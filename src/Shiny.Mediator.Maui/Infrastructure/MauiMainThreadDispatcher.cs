namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// MAUI <see cref="IMainThreadDispatcher"/> backed by <see cref="MainThread"/>.
/// </summary>
public class MauiMainThreadDispatcher : IMainThreadDispatcher
{
    /// <inheritdoc/>
    public bool IsMainThread => MainThread.IsMainThread;

    /// <inheritdoc/>
    public void BeginInvokeOnMainThread(Action action) => MainThread.BeginInvokeOnMainThread(action);
}
