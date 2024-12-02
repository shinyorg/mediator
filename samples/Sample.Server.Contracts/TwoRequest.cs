using Shiny.Mediator.Server;

namespace Sample.Server.Contracts;

public record TwoRequest : IServerRequest<DateTimeOffset>;