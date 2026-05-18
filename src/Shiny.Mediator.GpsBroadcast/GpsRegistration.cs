namespace Shiny.Mediator.GpsBroadcast;

/// <summary>
/// Extension methods for registering the Shiny GPS to mediator event bridge with a <see cref="ShinyMediatorBuilder"/>.
/// </summary>
public static class GpsRegistration
{
    /// <summary>
    /// Registers the GPS broadcaster so that incoming Shiny <see cref="Shiny.Locations.GpsReading"/> values are
    /// republished through the mediator as <see cref="GpsEvents"/>. Call this when configuring the mediator
    /// pipeline to enable <see cref="IGpsEventHandler"/> implementations.
    /// </summary>
    public static ShinyMediatorBuilder AddGpsBroadcast(this ShinyMediatorBuilder mediatorBuilder)
    {
        // mediatorBuilder.Services.AddGps<GpsBroadcaster>();
        return mediatorBuilder;
    }
}