using System.Collections.Generic;

namespace Shiny.Mediator.SourceGenerators;

// Additional strictness for opt-in OpenAPI contracts. Hand-written converters retain their
// existing behavior unless their caller explicitly supplies these schema facts.
internal sealed class JsonConverterGenerationOptions
{
    public bool IncludeInheritedProperties { get; set; }
    public bool UseInternalConverter { get; set; }
    public string? DiscriminatorPropertyName { get; set; }
    public string? DiscriminatorValue { get; set; }
    public ISet<string> RequiredProperties { get; } = new HashSet<string>();
    public ISet<string> NonNullProperties { get; } = new HashSet<string>();
    public ISet<string> OmitNullProperties { get; } = new HashSet<string>();
}
