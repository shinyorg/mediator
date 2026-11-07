using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Mediator;


/// <summary>
/// Uno <see cref="IServiceInitialize"/> that publishes a <see cref="ConnectivityChanged"/> event
/// through the mediator whenever the device's internet availability changes. Register it with
/// <c>AddConnectivityBroadcaster()</c>; Uno hosting initializes it once the host is built.
/// </summary>
public class ConnectivityBroadcaster(
    IMediator mediator,
    IInternetService internetService,
    ILogger<ConnectivityBroadcaster>? logger = null
) : IServiceInitialize
{
    int initialized;

    /// <inheritdoc/>
    public void Initialize()
    {
        if (Interlocked.Exchange(ref this.initialized, 1) == 1)
            return;

        internetService.StateChanged += async (_, connected) =>
        {
            try
            {
                await mediator.Publish(new ConnectivityChanged(connected)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred while broadcasting connectivity change");
            }
        };
    }
}
