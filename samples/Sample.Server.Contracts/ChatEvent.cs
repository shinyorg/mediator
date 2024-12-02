using Shiny.Mediator.Server;

namespace Sample.Server.Contracts;

public record ChatEvent(string From, string Message) : IServerEvent;