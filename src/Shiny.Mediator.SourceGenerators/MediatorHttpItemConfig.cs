namespace Shiny.Mediator.SourceGenerators;

public class MediatorHttpItemConfig
{
    public string Namespace { get; set; } = string.Empty;
    public string? ContractPrefix { get; set; }
    public string? ContractPostfix { get; set; }
    public bool GenerateModelsOnly { get; set; }
    public bool UseInternalClasses { get; set; }
    public bool GenerateJsonConverters { get; set; }
}