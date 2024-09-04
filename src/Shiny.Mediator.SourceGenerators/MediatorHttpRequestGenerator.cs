using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.CodeAnalysis;
using Shiny.Mediator.SourceGenerators.Http;

namespace Shiny.Mediator.SourceGenerators;


[Generator]
public class MediatorHttpRequestGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var rootNamespace = context.GetMSBuildProperty("RootNamespace")!;
        var items = context.GetAddtionalTexts("MediatorHttp");
        
        context.LogInfo("HTTP Generator - Item Count: " + items.Length);
        foreach (var item in items)
        {
            context
                .AnalyzerConfigOptions
                .GetOptions(item)
                .TryGetValue("build_metadata.AdditionalFiles.Namespace", out var nameSpace);

            nameSpace ??= rootNamespace;
            
            if (item.Path.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                this.Remote(context, item, nameSpace);
            else
                this.Local(context, item, nameSpace);
        }
    }

    void Local(GeneratorExecutionContext context, AdditionalText item, string nameSpace)
    {
        try
        {
            context.LogInfo($"Generating from local file '{item.Path}' with namespace '{nameSpace}'");

            var localCode = item.GetText(context.CancellationToken)!.ToString();
            var output = OpenApiContractGenerator.Generate(
                new MemoryStream(Encoding.UTF8.GetBytes(localCode)),
                nameSpace,
                e => context.LogInfo(e)
            );
            context.AddSource(nameSpace + ".g.cs", output);
        }
        catch (Exception ex)
        {
            context.LogError("Error generating local contract: " + ex);
        }
    }


    void Remote(GeneratorExecutionContext context, AdditionalText item, string nameSpace)
    {
        try
        {
            var remoteUri = new Uri(item.Path);
            context.LogInfo($"Generating for remote '{item.Path}' with namespace '{nameSpace}'");
            var http = new HttpClient { BaseAddress = remoteUri };
            var stream = http.GetStreamAsync(remoteUri).GetAwaiter().GetResult();

            var output = OpenApiContractGenerator.Generate(
                stream,
                nameSpace,
                e => context.LogInfo(e)
            );
            context.AddSource(nameSpace + ".g.cs", output);
        }
        catch (Exception ex)
        {
            context.LogError("Error generating remote HTTP contract: " + ex);
        }
    }
}