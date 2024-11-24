using System.Text.Json.Nodes;

namespace Shiny.Mediator.Server.Infrastructure;


public class ServerMessage
{
    public string Type { get; set; }
    public JsonObject Payload { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public DateTimeOffset? DateScheduled { get; set; }
    public bool IsEvent { get; set; }
}