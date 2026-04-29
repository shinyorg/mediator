using Shiny.Locations;

namespace Shiny.Mediator.GpsBroadcast;

public class GpsBroadcaster(IMediator mediator, IGpsManager gpsManager) : IGpsDelegate, IShinyStartupTask
{
    public void Start()
    {
    }

    public async Task OnReading(GpsReading reading)
    {
    }
}