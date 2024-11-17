namespace Shiny.Mediator.Server.Models;

public class Cluster
{
    // can have multiple logical clusters, but only one can process
    // a command or events
    public string Name { get; set; }
    
    // can send all messages here
    public DateTimeOffset LastOnline { get; set; }
    
    public string[] OwnedCommandTypes { get; set; }
    public string[] EventTypes { get; set; }
    
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}