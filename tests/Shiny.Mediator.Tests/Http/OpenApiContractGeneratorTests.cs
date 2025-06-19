using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests.Http;


public class OpenApiContractGeneratorTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("./Http/standard.json", "HttpRequest", "TestApi")]
    [InlineData("./Http/enums.json", null, "TestApi")]
    public Task Tests(string path, string? contractPostfix, string nameSpace)
    { 
        using var doc = File.OpenRead(path);
        var generator = new OpenApiContractGenerator(
            new MediatorHttpItemConfig
            {
                Namespace = nameSpace,
                ContractPostfix = contractPostfix
            },
            (msg, severity) => output.WriteLine($"[{severity}] {msg}")
        );
        var content = generator.Generate(doc);
        
        return Verify(content).UseParameters(path, contractPostfix, nameSpace);
    }

    
    [Theory]
    [InlineData("https://api.themeparks.wiki/docs/v1.yaml", "ThemeParksApi")]
    public async Task RemoteTests(string uri, string nameSpace)
    {
        var http = new HttpClient();
        var stream = await http.GetStreamAsync(uri);
        var cfg = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = "HttpRequest"
        };
        var generator = new OpenApiContractGenerator(cfg, (msg, level) => output.WriteLine(msg));
        var code = generator.Generate(stream);
    
        output.WriteLine(code);
    }
}