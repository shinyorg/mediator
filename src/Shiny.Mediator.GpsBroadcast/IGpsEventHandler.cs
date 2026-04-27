using Shiny.Locations;

namespace Shiny.Mediator.GpsBroadcast;

public record GpsEvents(GpsReading[] Readings) : IEvent;
public interface IGpsEventHandler : IEventHandler<GpsEvents>;