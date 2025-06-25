using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Microsoft.CodeAnalysis;
using Shiny.Mediator.SourceGenerators.Http;

namespace Shiny.Mediator.SourceGenerators;


[Generator(LanguageNames.CSharp)]
public class MediatorHttpRequestSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }


    public void Execute(GeneratorExecutionContext context)
    {
        var skip = context.GetMSBuildProperty("ShinyMediatorDisableSourceGen")?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;
        if (skip)
            return;
        
        var rootNamespace = context.GetMSBuildProperty("RootNamespace")!;
        var items = context.GetAddtionalTexts("MediatorHttp");
        
        context.LogInfo("HTTP Generator - Item Count: " + items.Length);
        foreach (var item in items)
        {
            try
            {
                var config = GetConfig(context, item, rootNamespace);
                
                if (config.Uri == null)
                    Local(context, item, config);
                else
                    Remote(context, item, config);
            }
            catch (Exception ex)
            {
                context.LogError("Error Generating HTTP Contracts: " + ex);
            }
        }
    }


    static MediatorHttpItemConfig GetConfig(GeneratorExecutionContext context, AdditionalText item, string rootNamespace)
    {
        var cfg = new MediatorHttpItemConfig
        {
            Namespace = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.Namespace)) ?? rootNamespace,
            ContractPrefix = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.ContractPrefix)),
            ContractPostfix = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.ContractPostfix)),
            GenerateModelsOnly = context
                .GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.GenerateModelsOnly))?
                .Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false,
            UseInternalClasses = context
                .GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.UseInternalClasses))?
                .Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false
        };
        
        var uri = context.GetAdditionalTextProperty(item, nameof(MediatorHttpItemConfig.Uri));
        if (!String.IsNullOrWhiteSpace(uri))
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var fullUri))
                cfg.Uri = fullUri;
            else
                throw new InvalidOperationException("Invalid URI: " + uri);
        }
        return cfg;
    }

    
    static void Local(GeneratorExecutionContext context, AdditionalText item, MediatorHttpItemConfig itemConfig)
    {
        context.LogInfo($"Generating from local file '{item.Path}' with namespace '{itemConfig.Namespace}'");
        
        var codeFile = item.GetText(context.CancellationToken);
        if (codeFile == null)
            throw new InvalidOperationException("No code file returned for " + item.Path);
        
        var localCode = codeFile.ToString();
        var generator = new OpenApiContractGenerator(itemConfig, (msg, level) => context.Log(msg, level));
        
        var output = generator.Generate(
            new MemoryStream(Encoding.UTF8.GetBytes(localCode))
        );
        
        context.AddSource(itemConfig.Namespace + ".g.cs", output);
    }


    static readonly HttpClient http = new();
    static void Remote(GeneratorExecutionContext context, AdditionalText item, MediatorHttpItemConfig itemConfig)
    {
        context.LogInfo($"Generating for remote '{itemConfig.Uri}' with namespace '{itemConfig.Namespace}'");
        var stream = http.GetStreamAsync(itemConfig.Uri).GetAwaiter().GetResult();

        var generator = new OpenApiContractGenerator(itemConfig, (msg, level) => context.Log(msg, level));
        var output = generator.Generate(stream);

        context.AddSource(itemConfig.Namespace + ".g.cs", output);
    }
}