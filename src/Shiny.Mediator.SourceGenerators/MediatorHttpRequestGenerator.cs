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
                var config = GetConfig(context, item, rootNamespace);
                
                if (item.Path.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    Remote(context, item, config);
                else
                    Local(context, item, config);
            }
            catch (Exception ex)
            {
                context.LogError("Error Generating HTTP Contracts: " + ex);
            }
        }
    }
    

    static MediatorHttpItemConfig GetConfig(GeneratorExecutionContext context, AdditionalText item, string rootNamespace)
        => new MediatorHttpItemConfig
        {
            Namespace = context.GetAdditionalTextProperty(item, "Namespace") ?? rootNamespace,
            ContractPrefix = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.ContractPrefix)),
            ContractPostfix = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.ContractPostfix))
        };
    
    
    static void Local(GeneratorExecutionContext context, AdditionalText item, MediatorHttpItemConfig itemConfig)
    {
        context.LogInfo($"Generating from local file '{item.Path}' with namespace '{itemConfig.Namespace}'");
        
        var codeFile = item.GetText(context.CancellationToken);
        if (codeFile == null)
            throw new InvalidOperationException("No code file returned for " + item.Path);

        var localCode = codeFile.ToString();
        var output = OpenApiContractGenerator.Generate(
            new MemoryStream(Encoding.UTF8.GetBytes(localCode)),
            itemConfig,
            e => context.LogInfo(e)
        );
        
        // TODO: could allow filename customization
        context.AddSource(itemConfig.Namespace + ".g.cs", output);
    }


    static void Remote(GeneratorExecutionContext context, AdditionalText item, MediatorHttpItemConfig itemConfig)
    {
        var remoteUri = new Uri(item.Path);
        context.LogInfo($"Generating for remote '{item.Path}' with namespace '{itemConfig.Namespace}'");
        var http = new HttpClient { BaseAddress = remoteUri };
        var stream = http.GetStreamAsync(remoteUri).GetAwaiter().GetResult();

        var output = OpenApiContractGenerator.Generate(
            stream,
            itemConfig,
            e => context.LogInfo(e)
        );
        // TODO: could allow filename customization
        context.AddSource(itemConfig.Namespace + ".g.cs", output);
    }
}