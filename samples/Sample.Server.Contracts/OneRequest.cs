using Shiny.Mediator.Server;

namespace Sample.Server.Contracts;

public record OneRequest : IServerRequest<DateTimeOffset>;