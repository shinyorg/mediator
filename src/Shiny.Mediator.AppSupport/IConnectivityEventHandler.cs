namespace Shiny.Mediator;

/// <summary>
/// Event published when network connectivity transitions between available and unavailable states.
/// </summary>
/// <param name="Connected"><c>true</c> when the device has internet access, otherwise <c>false</c>.</param>
public record ConnectivityChanged(bool Connected) : IEvent;

/// <summary>
/// Convenience event handler abstraction for subscribing to <see cref="ConnectivityChanged"/> notifications
/// without restating the generic parameter.
/// </summary>
public interface IConnectivityEventHandler : IEventHandler<ConnectivityChanged>;