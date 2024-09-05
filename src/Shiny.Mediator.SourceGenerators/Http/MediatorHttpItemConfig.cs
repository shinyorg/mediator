namespace Shiny.Mediator.SourceGenerators.Http;

public class MediatorHttpItemConfig
{
    public string? ContractPostfix { get; set; }
    public string? ContractPrefix { get; set; }
    public string Namespace { get; set; } = null!;
}