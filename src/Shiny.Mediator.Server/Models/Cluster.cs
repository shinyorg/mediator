namespace Shiny.Mediator.Server.Models;

// TODO: store stats on clusters
    // TODO: messages per second
    // TODO: online/offline states
public class Cluster
{
    // can have multiple logical clusters, but only one can process
    // a command or events
    public string Name { get; set; }
    
    // can send all messages here
    public DateTimeOffset LastOnline { get; set; }
    
    public string[] HandledRequestTypes { get; set; }
    public string[] PublishedEventTypes { get; set; }
    public string[] SubscribedEventTypes { get; set; }
    // TODO: cluster states
        // I own this/ese assemblies - which means only IT can process those requests or publish those events

    // TODO: client config (a cluster with send only requests or event handlers)
        // TODO: This/ese assemblies talk to this HOST
    
    public DateTimeOffset LastUpdated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}