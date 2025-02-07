using Windows.Networking.Connectivity;

namespace Shiny.Mediator.Infrastructure;


// https://platform.uno/docs/articles/features/windows-networking.html

public class InternetService : IInternetService
{
    EventHandler<bool>? handler;
    public event EventHandler<bool>? StateChanged
    {
        add
        {
            if (this.handler == null)
            {
                //NetworkInformation.NetworkStatusChanged += handler;
            }
            this.handler += value;
        }
        remove
        {
            //NetworkInformation.NetworkStatusChanged -= handler;
        }
    }

    public bool IsAvailable
    {
        get
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            if (profile == null)
                return false;

            var level = profile.GetNetworkConnectivityLevel();
            return level == NetworkConnectivityLevel.InternetAccess;
        }
    }
    
    
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;
        
        var tcs = new TaskCompletionSource();
        var handler = new NetworkStatusChangedEventHandler(_ => 
        {
            if (this.IsAvailable)
                tcs.TrySetResult();
        });
        try
        {
            using var _ = cancelToken.Register(() => tcs.TrySetCanceled());
            NetworkInformation.NetworkStatusChanged += handler;
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            NetworkInformation.NetworkStatusChanged -= handler;    
        }
    }
}