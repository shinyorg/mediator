using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;

static class Extensions
{
    const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";


    public static string? GetMSBuildProperty(this GeneratorExecutionContext context, string propertyName)
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value);
        return value;
    }


    public static string[] GetMSBuildItems(this GeneratorExecutionContext context, string name) => context
        .AdditionalFiles
        .Where(x => 
            context
                .AnalyzerConfigOptions
                .GetOptions(x)
                .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup) && 
            sourceItemGroup == name
        )
        .Select(x => x.Path)
        .ToArray();
    
    public static AdditionalText[] GetAddtionalTexts(this GeneratorExecutionContext context, string name)
        => context
            .AdditionalFiles
            .Where(x => 
                context
                    .AnalyzerConfigOptions
                    .GetOptions(x)
                    .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup) && 
                sourceItemGroup == name
            )
            .ToArray();
}