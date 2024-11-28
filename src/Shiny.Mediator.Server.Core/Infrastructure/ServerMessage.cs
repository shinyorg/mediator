using System.Text.Json.Nodes;

namespace Shiny.Mediator.Server.Infrastructure;


public record ServerMessage(
    string Type,
    JsonObject Payload,
    DateTimeOffset? Expires,
    DateTimeOffset? DateScheduled 
);