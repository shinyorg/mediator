using System;
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
        // context.CancellationToken
        var items = context.GetItemGroup<MediatorHttpItem>("MediatorHttp");
        foreach (var item in items)
        {
            // if file - the file has already been read
            if (item.Uri != null)
            {
                var http = new HttpClient { BaseAddress = new Uri(item.Uri) };
                var stream = http.GetStreamAsync(item.Uri!).GetAwaiter().GetResult();
                
                var output = OpenApiContractGenerator.Generate(
                    stream,
                    item.Namespace!,
                    e => { }
                );
                context.AddSource(item.Namespace + ".g.cs", output);
            }
            else
            {
                var output = OpenApiContractGenerator.Generate(
                    new MemoryStream(Encoding.UTF8.GetBytes(item.LocalContent!)),
                    item.Namespace!,
                    e => { }
                );
                context.AddSource(item.Namespace + ".g.cs", output);
            }
        }
    }
}