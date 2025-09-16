using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Shiny.Mediator.SourceGenerators.Http;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class MediatorHttpRequestSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) => provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value) ? value : "");

        var mediatorHttpItems = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair =>
            {
                var (text, configOptions) = pair;
                var options = configOptions.GetOptions(text);

                if (!options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var value))
                    return false;

                var mediatorItem = value.Equals("MediatorHttp", StringComparison.InvariantCultureIgnoreCase);
                return mediatorItem;
            })
            .Select((pair, _) => pair.Left)
            .Collect();

        var combined = mediatorHttpItems
            .Combine(rootNamespace)
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, (sourceContext, data) =>
        {
            var (((texts, rootNs), configOptions), compilation) = data;
            
            foreach (var item in texts)
            {
                try
                {
                    var config = GetConfig(configOptions, item, rootNs);
                    
                    if (config.Uri == null)
                        Local(sourceContext, item, config, compilation);
                    else
                        Remote(sourceContext, item, config, compilation);
                }
                catch (Exception ex)
                {
                    sourceContext.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "SHINYMED000",
                            "Error Generating HTTP Contracts",
                            "Error Generating HTTP Contracts: {0}",
                            "ShinyMediator",
                            DiagnosticSeverity.Error,
                            true),
                        Location.None,
                        ex.ToString())
                    );
                }
            }
        });
    }

    static MediatorHttpItemConfig GetConfig(AnalyzerConfigOptionsProvider configProvider, AdditionalText item, string rootNamespace)
    {
        var cfg = new MediatorHttpItemConfig
        {
            Namespace = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.Namespace)) ?? rootNamespace,
            ContractPrefix = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.ContractPrefix)),
            ContractPostfix = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.ContractPostfix)),
            GenerateModelsOnly = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.GenerateModelsOnly))
                ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            UseInternalClasses = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.UseInternalClasses))
                ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            GenerateJsonConverters = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.GenerateJsonConverters))
                ?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false
        };
        
        var uri = GetAdditionalTextProperty(configProvider, item, nameof(MediatorHttpItemConfig.Uri));
        if (!String.IsNullOrWhiteSpace(uri))
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var fullUri))
                cfg.Uri = fullUri;
            else
                throw new InvalidOperationException("Invalid URI: " + uri);
        }
        return cfg;
    }

    static string? GetAdditionalTextProperty(AnalyzerConfigOptionsProvider configProvider, AdditionalText item, string propertyName)
    {
        if (configProvider.GetOptions(item).TryGetValue($"build_metadata.AdditionalFiles.{propertyName}", out var value))
            return value;
        return null;
    }

    static void Local(SourceProductionContext context, AdditionalText item, MediatorHttpItemConfig itemConfig, Compilation compilation)
    {
        var codeFile = item.GetText(context.CancellationToken);
        if (codeFile == null)
            throw new InvalidOperationException("No code file returned for " + item.Path);
        
        var localCode = codeFile.ToString();
        var generator = new OpenApiContractGenerator(
            itemConfig,
            (msg, severity) => ReportMessage(context, msg, severity),
            x => ProcessFileRequest(context, itemConfig, compilation, x)
        );
        generator.Generate(
            new MemoryStream(Encoding.UTF8.GetBytes(localCode))
        );
    }

    static readonly HttpClient Http = new();
    
    static void Remote(SourceProductionContext context, AdditionalText _, MediatorHttpItemConfig itemConfig, Compilation compilation)
    {
        var stream = Http.GetStreamAsync(itemConfig.Uri).GetAwaiter().GetResult();

        var generator = new OpenApiContractGenerator(
            itemConfig, 
            (msg, severity) => ReportMessage(context, msg, severity),
            x => ProcessFileRequest(context, itemConfig, compilation, x)
        );
        generator.Generate(stream);
    }

    
    static void ReportMessage(SourceProductionContext context, string message, DiagnosticSeverity severity)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "SHINYMED001",
                "HTTP Generator Message",
                "{0}",
                "ShinyMediator",
                severity,
                true
            ),
            Location.None,
            message
        ));
    }


    static void ProcessFileRequest(
        SourceProductionContext context, 
        MediatorHttpItemConfig itemConfig, 
        Compilation compilation,
        FileRequest request
    )
    {
        context.AddSource(request.TypeName + ".g.cs", request.Content);
        if (itemConfig.GenerateJsonConverters && request.IsRemoteObject)
        {
            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(request.Content);
                var newCompilation = compilation.AddSyntaxTrees(syntaxTree);
                var typeSymbol = newCompilation.GetTypeByMetadataName(request.TypeName);
                if (typeSymbol != null)
                    JsonConverterSourceGenerator.GenerateJsonConverter(context, typeSymbol);
            }
            catch (Exception e)
            {
                // context.ReportDiagnostic(Diagnostic.Create(
                //     new DiagnosticDescriptor(
                //         "SHINYMED002",
                //         "Error Generating JSON Converter",
                //         "Error Generating JSON Converter: {0}",
                //         "ShinyMediator",
                //         DiagnosticSeverity.Error,
                //         true
                //     ),
                //     Location.None,
                //     e.ToString()
                // ));
            }
        }
    }
}
