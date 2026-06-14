using System.Text.Json.Serialization;

namespace Shiny.Mediator.Tests.SourceGeneration.TestModels;


// The reported failing shape: a record declared with a body and init-only properties (NO positional
// primary constructor) plus [JsonPropertyName] remapping. The source-generated converter must
// construct it via `new AnchorPointDto { X = x, Y = y }` — the old generator emitted
// `new AnchorPointDto(x, y)`, which does not compile (CS1729) because there is no such constructor.
// Simply having this type in the test assembly is a build-time regression guard.
[SourceGenerateJsonConverter]
public partial record AnchorPointDto
{
    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }
}


// Identical shape with no generated converter — used to confirm System.Text.Json handles the
// init-only record + [JsonPropertyName] on its own (the baseline the generator must match).
public record AnchorPointStj
{
    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }
}
