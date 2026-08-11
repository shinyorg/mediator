using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Mediator;


/// <summary>
/// Uno <see cref="IServiceInitialize"/> that publishes a <see cref="ConnectivityChanged"/> event
/// through the mediator whenever the device's internet availability changes.
/// </summary>
public class ConnectivityBroadcaster(
    IMediator mediator,
    IInternetService internetService,
    ILogger<ConnectivityBroadcaster>? logger = null
) : IServiceInitialize
{
    /// <inheritdoc/>
    public void Initialize()
    {
        internetService.StateChanged += async (_, connected) =>
        {
            try
            {
                await mediator.Publish(new ConnectivityChanged(connected));
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occured while connectivity Sprayer");
            }
        };
    }
}

/// <summary>
/// Mediator event published by <see cref="ConnectivityBroadcaster"/> when the device's internet
/// availability changes; <c>Connected</c> reflects the new state.
/// </summary>
public record ConnectivityChanged(bool Connected) : IEvent;