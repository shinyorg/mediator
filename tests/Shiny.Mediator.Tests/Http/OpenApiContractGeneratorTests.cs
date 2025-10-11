using System.Runtime.CompilerServices;
using System.Text;
using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Http;


public record PathAndMediatorHttpItemConfig(string Path, MediatorHttpItemConfig Config);

public class OpenApiContractGeneratorTests(ITestOutputHelper output)
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifierSettings.NameForParameter<PathAndMediatorHttpItemConfig>(x =>
            $"{Path.GetFileName(x.Path)}_{x.Config.Namespace}_{x.Config.ContractPostfix}_{x.Config.UseInternalClasses}_{x.Config.GenerateModelsOnly}_{x.Config.GenerateJsonConverters}_{x.Config.GenerateRequestsOnly}"
        );
    }
    
    [Theory]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, false, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", true, false, false)]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi", false, true, false)]
    [InlineData("./Http/enums.json", null, "TestApi", false, false, false)]
    [InlineData("./Http/validateresult.json", null, "TestApi", false, false, false)]
    public Task Local_Tests(
        string path, 
        string? contractPostfix, 
        string nameSpace, 
        bool useInternalClasses,
        bool generateModelsOnly,
        bool generateRequestsOnly
    )
    {
        var args = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = contractPostfix,
            UseInternalClasses = useInternalClasses,
            GenerateModelsOnly = generateModelsOnly,
            GenerateRequestsOnly = generateRequestsOnly
        };

        var sb = new StringBuilder();
        using var doc = File.OpenRead(path);
        var generator = new OpenApiContractGenerator(
            args, 
            (msg, severity) => output.WriteLine($"[{severity}] {msg}"),
            x =>
            {
                output.WriteLine($"Type: {x.TypeName} - Remote Object: {x.IsRemoteObject}");
                sb.Append(x.Content);
            }
        );
        generator.Generate(doc);
        
        return Verify(sb.ToString())
            .UseParameters(new PathAndMediatorHttpItemConfig(path, args));
    }
    
    
    [Theory]
    [InlineData("https://api.themeparks.wiki/docs/v1.yaml", "ThemeParksApi")]
    public async Task RemoteTests(string uri, string nameSpace)
    {
        var http = new HttpClient();
        var stream = await http.GetStreamAsync(uri);
        var sb = new StringBuilder();
        
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = nameSpace,
                ContractPostfix = "HttpRequest"
            }, 
            (msg, severity) => output.WriteLine($"[{severity}] {msg}"),
            x =>
            {
                output.WriteLine($"Type: {x.TypeName} - Remote Object: {x.IsRemoteObject}");
                sb.Append(x.Content);
            }
        );
        generator.Generate(stream);
    
        await Verify(sb.ToString()).UseParameters(uri, nameSpace);
    }
}