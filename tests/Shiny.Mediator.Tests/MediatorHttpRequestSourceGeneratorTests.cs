using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Shiny.Mediator.SourceGenerators;

namespace Shiny.Mediator.Tests;

public class MediatorHttpRequestSourceGeneratorTests
{
    [Fact(Skip = "TODO")]
    public void Generate_HttpContracts_Remote()
    {
    }
    
    
    [Fact(Skip = "TODO")]
    public void Generate_HttpContracts_Local()
    {
        
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
     */
    
    static GeneratorDriver BuildDriver(Assembly metadataAssembly, params IEnumerable<(string Key, string Value)> additionalFileKeys)
    {
        var metadataReference = MetadataReference.CreateFromFile(metadataAssembly.Location);
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestAssembly", [], [metadataReference], options);
        // add code
    
        var generator = new MediatorSourceGenerator();
        
        var dict = additionalFileKeys.ToDictionary(x => "build_metadata.AdditionalFiles." + x.Key, x => x.Value, comparer: StringComparer.InvariantCultureIgnoreCase);
        var provider = new MockAnalyzerConfigOptionsProvider(dict);
    
        
        // new AdditionalTextValueProvider<>()
        var driver = CSharpGeneratorDriver.Create([generator], optionsProvider: provider);
        return driver.RunGenerators(compilation);
    }
}

// public class MediatorAdditionalText : AdditionalText
// {
//     public override SourceText? GetText(CancellationToken cancellationToken = new CancellationToken())
//     {
//         throw new NotImplementedException();
//     }
//
//     public override string Path { get; }
// }