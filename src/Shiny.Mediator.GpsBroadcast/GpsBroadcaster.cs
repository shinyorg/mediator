using Shiny.Locations;

namespace Shiny.Mediator.GpsBroadcast;

/// <summary>
/// Shiny GPS delegate that forwards <see cref="GpsReading"/> callbacks from <see cref="IGpsManager"/>
/// into the mediator pipeline as <see cref="GpsEvents"/>. Runs as an <see cref="IShinyStartupTask"/>
/// so that the bridge is wired up at application startup.
/// </summary>
public class GpsBroadcaster(IMediator mediator, IGpsManager gpsManager) : IGpsDelegate, IShinyStartupTask
{
    /// <inheritdoc/>
    public void Start()
    {
    }

    /// <inheritdoc/>
    public async Task OnReading(GpsReading reading)
    {
    }
}