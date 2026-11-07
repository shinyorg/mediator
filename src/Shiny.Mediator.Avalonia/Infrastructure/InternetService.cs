using System.Net.NetworkInformation;

namespace Shiny.Mediator.Infrastructure;


/// <summary>
/// Avalonia <see cref="IInternetService"/> backed by <see cref="NetworkChange"/> and
/// <see cref="NetworkInterface.GetIsNetworkAvailable"/>. It reports whether any network interface is
/// up, not whether the internet is reachable through it. In the browser, where these APIs are not
/// supported, the network is always reported as available. Register your own
/// <see cref="IInternetService"/> before <c>UseAvalonia()</c> for a platform-specific check.
/// </summary>
public class InternetService : IInternetService
{
    readonly Lock sync = new();
    EventHandler<bool>? handler;

    static bool IsSupported => !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();


    /// <inheritdoc/>
    public event EventHandler<bool>? StateChanged
    {
        add
        {
            lock (this.sync)
            {
                if (this.handler == null && IsSupported)
                    NetworkChange.NetworkAvailabilityChanged += this.OnNetworkAvailabilityChanged;

                this.handler += value;
            }
        }
        remove
        {
            lock (this.sync)
            {
                this.handler -= value;
                if (this.handler == null && IsSupported)
                    NetworkChange.NetworkAvailabilityChanged -= this.OnNetworkAvailabilityChanged;
            }
        }
    }


    /// <inheritdoc/>
    public bool IsAvailable => !IsSupported || NetworkInterface.GetIsNetworkAvailable();


    /// <inheritdoc/>
    public async Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        if (this.IsAvailable)
            return;

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiter = new NetworkAvailabilityChangedEventHandler((_, args) =>
        {
            if (args.IsAvailable)
                tcs.TrySetResult();
        });
        try
        {
            using var _ = cancelToken.Register(() => tcs.TrySetCanceled(cancelToken));
            NetworkChange.NetworkAvailabilityChanged += waiter;

            // availability may have changed before the handler was attached
            if (this.IsAvailable)
                tcs.TrySetResult();

            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            NetworkChange.NetworkAvailabilityChanged -= waiter;
        }
    }


    void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs args)
        => this.handler?.Invoke(this, args.IsAvailable);
}
