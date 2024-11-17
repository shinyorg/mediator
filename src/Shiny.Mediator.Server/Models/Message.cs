using System.Text.Json.Nodes;

namespace Shiny.Mediator.Server.Models;


public class Message
{
    public Guid MessageId { get; set; }
    public string FromClusterId { get; set; }
    
    public JsonObject Payload { get; set; }
    public DateTimeOffset Received { get; set; }
    public DateTimeOffset Expires { get; set; }
    public DateTimeOffset? Processed { get; set; }
    public DateTimeOffset? DateScheduled { get; set; }
}