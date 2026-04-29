namespace Shiny.Mediator.GpsBroadcast;

public static class GpsRegistration
{
    public static ShinyMediatorBuilder AddGpsBroadcast(this ShinyMediatorBuilder mediatorBuilder)
    {
        // mediatorBuilder.Services.AddGps<GpsBroadcaster>();
        return mediatorBuilder;
    }
}