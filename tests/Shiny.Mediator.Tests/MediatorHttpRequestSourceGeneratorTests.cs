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
    // could do a theory with several urls
    [Fact(Skip = "Not finding items - needs more investigation")]
    public Task Generate_HttpContracts_Remote_Yaml()
    {
        var driver = BuildDriver(this.GetType().Assembly, "OpenApiRemote", "MyTests", "https://api.themeparks.wiki/docs/v1.yaml");
        var result = driver.GetRunResult().Results.FirstOrDefault();
        result.Exception.ShouldBeNull();
        return Verify(result);
    }
    
    
    [Fact(Skip = "TODO")]
    public void Generate_HttpContracts_Local()
    {
        // var driver = BuildDriver(this.GetType().Assembly, "openapi.json", "MyTests");
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
        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);

        var text = new VoidAdditionalText(remoteNameOrLocalFile);
        driver.AddAdditionalTexts([text]);
        
        return driver.RunGenerators(compilation);
    }
}

class VoidAdditionalText(string path) : AdditionalText
{
    public override string Path => path;
    public override SourceText GetText(CancellationToken cancellationToken = default) => null;
}
/*
    <!--how to test?-->
    <ItemGroup>
       <MediatorHttp Include="OpenApiRemote"
                     Uri="https://api.themeparks.wiki/docs/v1.yaml"
                     Namespace="Sample.ThemeParksApi"
                     ContractPostfix="HttpRequest"
                     Visible="false" />
    </ItemGroup>

        public static string? GetAdditionalTextProperty(this GeneratorExecutionContext context, AdditionalText text, string name)
       {
           context
               .AnalyzerConfigOptions
               .GetOptions(text)
               .TryGetValue($"build_metadata.AdditionalFiles.{name}", out var value);

           return value;
       }


       const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";
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


                       <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="SourceItemGroup" Visible="false" />
       <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="Namespace" Visible="false" />
       <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="Uri" Visible="false" />
       <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="ContractPrefix" Visible="false" />
       <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="ContractPostfix" Visible="false" />
     */
