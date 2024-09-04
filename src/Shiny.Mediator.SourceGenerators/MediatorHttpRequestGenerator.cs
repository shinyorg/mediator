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
        context.AddSource("Test.g.cs", "public class Test {}");
        
        foreach (var item in items)
        {
            context
                .AnalyzerConfigOptions
                .GetOptions(item)
                .TryGetValue("build_metadata.AdditionalFiles.Namespace", out var nameSpace);

            nameSpace ??= rootNamespace;
            
            Uri.TryCreate(item.Path, UriKind.Absolute, out var remoteUri);
            if (remoteUri == null)
            {
                var localCode = item.GetText(context.CancellationToken)!.ToString();
                var output = OpenApiContractGenerator.Generate(
                    new MemoryStream(Encoding.UTF8.GetBytes(localCode)),
                    nameSpace,
                    e => Debug.WriteLine(e)
                );
                context.AddSource(nameSpace + ".g.cs", output);
            }
            else
            {
                var http = new HttpClient { BaseAddress = remoteUri };
                var stream = http.GetStreamAsync(remoteUri).GetAwaiter().GetResult();
                
                var output = OpenApiContractGenerator.Generate(
                    stream,
                    nameSpace,
                    e => Debug.WriteLine(e)
                );
                context.AddSource(nameSpace + ".g.cs", output);
            }
        }
    }
}