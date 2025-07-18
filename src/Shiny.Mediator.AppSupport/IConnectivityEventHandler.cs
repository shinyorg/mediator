namespace Shiny.Mediator;

public record ConnectivityChanged(bool Connected) : IEvent;
public interface IConnectivityEventHandler : IEventHandler<ConnectivityChanged>;