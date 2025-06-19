using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shiny.Mediator.SourceGenerators;
using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Http;

public class MediatorHttpRequestSourceGeneratorTests(ITestOutputHelper output)
{
    // TODO: could do a theory with several urls
    [Fact]
    public Task Generate_HttpContracts_Remote_Yaml()
    {
        var driver = BuildDriver(this.GetType().Assembly, "OpenApiRemote", "MyTests", "https://api.themeparks.wiki/docs/v1.yaml");
        var results = driver.GetRunResult();
        return Verify(results);
    }

    [Fact]
    public Task Generate_HttpContracts_Local()
    {
        // tests enums and timespans
        var file = new FileInfo("./Http/testapi.json");
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = "MyTests",
                ContractPrefix = "HttpRequest",
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        
        file.Exists.ShouldBeTrue("Could not find file 'testapi.json'.");
        var content = generator.Generate(file.OpenRead());
        return Verify(content);
    }
    
    
    static GeneratorDriver BuildDriver(
        Assembly metadataAssembly, 
        string remoteNameOrLocalFile,
        string generatedNamespace, 
        string? remoteUri
    )
    {
        var metadataReference = MetadataReference.CreateFromFile(metadataAssembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [], [metadataReference], options);
    
        var generator = new MediatorHttpRequestSourceGenerator();
        var dict = new Dictionary<string, string>
        {
            { "build_metadata.AdditionalFiles.SourceItemGroup", "MediatorHttp" },
            { "build_metadata.AdditionalFiles.Namespace", generatedNamespace }
            // { "build_metadata.AdditionalFiles.ContractPrefix", "" },
            // { "build_metadata.AdditionalFiles.ContractPostfix", "" }
        };
        if (!String.IsNullOrWhiteSpace(remoteUri)) 
            dict.Add("build_metadata.AdditionalFiles.Uri", remoteUri!);
        
        var provider = new MockAnalyzerConfigOptionsProvider(dict);
        var additionalText = new VoidAdditionalText(remoteNameOrLocalFile);
        var driver = CSharpGeneratorDriver.Create([generator], additionalTexts: [additionalText], optionsProvider: provider);

        return driver.RunGenerators(compilation);
    }
}

class VoidAdditionalText(string path) : AdditionalText
{
    public override string Path => path;
    public override SourceText GetText(CancellationToken cancellationToken = default) => null;
}