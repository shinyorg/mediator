using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;


static class Extensions
{
    public static string? GetMSBuildProperty(this GeneratorExecutionContext context, string propertyName)
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value);
        return value;
    }    
}