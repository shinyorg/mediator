using System.Text.Json.Nodes;

namespace Shiny.Mediator.Server.Infrastructure;

public record ServerResult(Guid Id, JsonObject Payload);