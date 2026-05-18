using Shiny.Locations;

namespace Shiny.Mediator.GpsBroadcast;

/// <summary>
/// Mediator event published when one or more Shiny GPS readings are received.
/// Subscribers receive the batched <see cref="GpsReading"/> values forwarded from the underlying <see cref="IGpsManager"/>.
/// </summary>
/// <param name="Readings">The GPS readings included in this broadcast.</param>
public record GpsEvents(GpsReading[] Readings) : IEvent;

/// <summary>
/// Marker handler interface for components that react to <see cref="GpsEvents"/> published by the GPS broadcaster.
/// Implement this interface and register it with the DI container to receive GPS readings through the mediator pipeline.
/// </summary>
public interface IGpsEventHandler : IEventHandler<GpsEvents>;