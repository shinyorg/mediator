namespace Shiny.Mediator.Tests.SourceGeneration.TestModels;


public enum Status
{
    Open,
    Closed
}


[SourceGenerateJsonConverter]
public partial class Entity
{
    public Status Required { get; set; }
    public Status? Optional { get; set; }
}
