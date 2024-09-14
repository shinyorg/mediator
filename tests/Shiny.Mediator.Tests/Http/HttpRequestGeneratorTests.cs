using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpRequestGeneratorTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("./Http/test.json", "TestApi")]
    public void Tests(string path, string nameSpace)
    { 
        using var doc = File.OpenRead(path);
        var item = new MediatorHttpItemConfig { Namespace = nameSpace };
        var code = OpenApiContractGenerator.Generate(doc, item, e => output.WriteLine(e));
        
        output.WriteLine(code);
    }
}