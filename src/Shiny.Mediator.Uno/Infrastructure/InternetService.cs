using Windows.Networking.Connectivity;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Uno <see cref="IInternetService"/> backed by <see cref="NetworkInformation"/>. Subscribes to
/// <see cref="NetworkInformation.NetworkStatusChanged"/> only while there are active
/// <see cref="StateChanged"/> subscribers.
/// </summary>
public class InternetService : IInternetService
{
    EventHandler<bool>? handler;

    /// <inheritdoc/>
    public event EventHandler<bool>? StateChanged
    {
        add
        {
            if (this.handler == null)
            {
                NetworkInformation.NetworkStatusChanged += this.OnNetowrkStatusChanged;
            }
            this.handler += value;
        }
        remove
        {
            this.handler -= value;
            if (this.handler == null)
            {
                NetworkInformation.NetworkStatusChanged -= this.OnNetowrkStatusChanged;
            }
        }
    }

    void OnNetowrkStatusChanged(object sender) => this.handler?.Invoke(sender, this.IsAvailable);


    /// <inheritdoc/>
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


    /// <inheritdoc/>
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