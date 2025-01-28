namespace Shiny.Mediator.Infrastructure;


public class InternetService(IConnectivity connectivity) : IInternetService
{
    EventHandler<bool>? handler;
    public event EventHandler<bool>? StateChanged
    {
        add
        {
            if (this.handler == null)
            {
                // connectivity.ConnectivityChanged += handler;
            }
            this.handler += value;
        }
        remove
        {
            // connectivity.ConnectivityChanged -= handler;
        }
    }
    public bool IsAvailable => connectivity.NetworkAccess == NetworkAccess.Internet;
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
}