namespace Shiny.Mediator.Server.Infrastructure;

public class ClusterRegistration
{
    public string ClusterName { get; set; }
    public List<string> OwnedCommandTypes { get; set; }
    public List<string> EventTypes { get; set; }
}