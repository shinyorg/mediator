using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Shiny.Mediator.SourceGenerators;

static class Extensions
{
    const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    internal const string GeneratedCodeAttribute = "[global::System.CodeDom.Compiler.GeneratedCode(\"Shiny.Mediator\", \"4.0.0\")]";
    
    public static string Pascalize(this string str)
    {
        if (str.All(x => char.IsUpper(x) || !char.IsLetter(x)))
        {
            if (str.Contains("_"))
            {
                var pascal = str.ToLower()
                    .Split(["_"], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                    .Aggregate(string.Empty, (s1, s2) => s1 + s2);

                return pascal;
            }

            var result = char.ToUpper(str[0]) + str.Substring(1).ToLower();
            return result;
        }

        if (char.IsUpper(str[0]))
            return str;
        
        var r = char.ToUpper(str[0]) + str.Substring(1);
        r = r.Replace("_", "");
        return r;
    }

    public static string? GetMSBuildProperty(this GeneratorExecutionContext context, string propertyName)
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value);
        return value;
    }


    public static void Log(this GeneratorExecutionContext context, string message, DiagnosticSeverity severity)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "SHINYMED000",
                "Mediator",
                message,
                "Shiny.Mediator",
                severity,
                true
            ),
            Location.None
        ));
    }

    public static void LogInfo(this GeneratorExecutionContext context, string message)
        => context.Log(message, DiagnosticSeverity.Info);
    
    public static void LogError(this GeneratorExecutionContext context, string message)
        => context.Log(message, DiagnosticSeverity.Error);
    
    public static string? GetAdditionalTextProperty(this GeneratorExecutionContext context, AdditionalText text, string name)
    {
        context
            .AnalyzerConfigOptions
            .GetOptions(text)
            .TryGetValue($"build_metadata.AdditionalFiles.{name}", out var value);

        return value;
    }
    
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