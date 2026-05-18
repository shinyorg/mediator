namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// MAUI <see cref="IInternetService"/> backed by <see cref="IConnectivity"/>. Subscribes to
/// <see cref="IConnectivity.ConnectivityChanged"/> only while there are active <see cref="StateChanged"/>
/// subscribers.
/// </summary>
public class InternetService(IConnectivity connectivity) : IInternetService
{
    EventHandler<bool>? handler;

    /// <inheritdoc/>
    public event EventHandler<bool>? StateChanged
    {
        add
        {
            if (this.handler == null)
            {
                connectivity.ConnectivityChanged += this.OnConnectivityChanged;
            }
            this.handler += value;
        }
        remove
        {
            this.handler -= value;
            if (this.handler == null)
            {
                connectivity.ConnectivityChanged -= this.OnConnectivityChanged;
            }
        }
    }
    
    
    
    /// <inheritdoc/>
    public bool IsAvailable => connectivity.NetworkAccess == NetworkAccess.Internet;

    /// <inheritdoc/>
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;
        
        var tcs = new TaskCompletionSource();
        var handler = new EventHandler<ConnectivityChangedEventArgs>((sender, args) =>
        {
            if (args.NetworkAccess == NetworkAccess.Internet)
                tcs.TrySetResult();
        });
        try
        {
            using var _ = cancelToken.Register(() => tcs.TrySetCanceled());
            connectivity.ConnectivityChanged += handler;
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            connectivity.ConnectivityChanged -= handler;    
        }
    }


    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs args)
    {
        var connected = args.NetworkAccess == NetworkAccess.Internet;
        this.handler?.Invoke(null, connected);
    }
}