namespace Shiny.Mediator.Server.Client;

public interface IServerEvent : IEvent
{
    DateTimeOffset CreatedAt { get; }
}