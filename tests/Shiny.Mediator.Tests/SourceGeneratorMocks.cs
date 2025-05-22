using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.Mediator.Tests;

public class MockAnalyzerConfigOptionsProvider(Dictionary<string, string> buildProperties) : AnalyzerConfigOptionsProvider
{
    readonly MockAnalyzerConfigOptions options = new (buildProperties);

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => this.options;
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)  => this.options;
    public override AnalyzerConfigOptions GlobalOptions => this.options;
}

public class MockAnalyzerConfigOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
{
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        => values.TryGetValue(key, out value);

    public override IEnumerable<string> Keys => values.Keys;
}