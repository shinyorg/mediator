using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Shiny.Mediator.SourceGenerators;
using Shiny.Mediator.SourceGenerators.Http;

namespace Shiny.Mediator.Tests;

public class MediatorHttpRequestSourceGeneratorTests
{
    // TODO: could do a theory with several urls
    [Fact]
    public Task Generate_HttpContracts_Remote_Yaml()
    {
        var driver = BuildDriver(this.GetType().Assembly, "OpenApiRemote", "MyTests", "https://api.themeparks.wiki/docs/v1.yaml");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }
    
    
    [Fact(Skip = "TODO")]
    public Task Generate_HttpContracts_Local()
    {
        // var driver = BuildDriver(this.GetType().Assembly, "openapi.json", "MyTests");
        return Task.CompletedTask; // Verify
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
/*
<ItemGroup>
   <MediatorHttp Include="OpenApiRemote"
                 Uri="https://api.themeparks.wiki/docs/v1.yaml"
                 Namespace="Sample.ThemeParksApi"
                 ContractPostfix="HttpRequest"
                 Visible="false" />
</ItemGroup>
<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="SourceItemGroup" Visible="false" />
<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="Namespace" Visible="false" />
<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="Uri" Visible="false" />
<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="ContractPrefix" Visible="false" />
<CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="ContractPostfix" Visible="false" />
*/
