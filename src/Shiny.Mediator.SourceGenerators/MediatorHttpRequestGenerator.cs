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
            try
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
            catch (Exception ex)
            {
                context.LogError("Error Generating HTTP Contracts: " + ex);
            }
        }
    }


    void Local(GeneratorExecutionContext context, AdditionalText item, string nameSpace)
    {
        context.LogInfo($"Generating from local file '{item.Path}' with namespace '{nameSpace}'");
        
        var codeFile = item.GetText(context.CancellationToken);
        if (codeFile == null)
            throw new InvalidOperationException("No code file returned for " + item.Path);

        var localCode = codeFile.ToString();
        var output = OpenApiContractGenerator.Generate(
            new MemoryStream(Encoding.UTF8.GetBytes(localCode)),
            nameSpace,
            e => context.LogInfo(e)
        );
        context.AddSource(nameSpace + ".g.cs", output);
    }


    void Remote(GeneratorExecutionContext context, AdditionalText item, string nameSpace)
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
}