using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpRequestGeneratorTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("./Http/test.json", "TestApi")]
    [InlineData("./Http/consumerApiV1.json", "ConsumerApi")]
    public void Tests(string path, string nameSpace)
    { 
        using var doc = File.OpenRead(path);
        var item = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = "HttpRequest"
        };
        var code = OpenApiContractGenerator.Generate(doc, item, e => output.WriteLine(e));
        
        output.WriteLine(code);
    }
}