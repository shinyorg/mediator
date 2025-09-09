using System;

namespace Shiny.Mediator.SourceGenerators.Http;

public class MediatorHttpItemConfig
{
    public string? ContractPostfix { get; set; }
    public string? ContractPrefix { get; set; }
    public string Namespace { get; set; } = null!;
    public bool UseInternalClasses { get; set; }
    public bool GenerateModelsOnly { get; set; }
    public bool GenerateJsonConverters { get; set; }
    public Uri? Uri { get; set; }
}