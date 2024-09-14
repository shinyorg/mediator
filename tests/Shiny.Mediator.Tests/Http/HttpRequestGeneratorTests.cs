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
        var item = new MediatorHttpItemConfig
        {
            Namespace = nameSpace,
            ContractPostfix = "HttpRequest"
        };
        var code = OpenApiContractGenerator.Generate(doc, item, e => output.WriteLine(e));
        output.WriteLine(code);
    }

    // [Theory]
    // [InlineData("", "TestApi")]
    // public async Task RemoteTests(string uri, string nameSpace)
    // {
    //     var http = new HttpClient();
    //     var stream = await http.GetStreamAsync(uri);
    //     var cfg = new MediatorHttpItemConfig
    //     {
    //         Namespace = nameSpace,
    //         ContractPostfix = "HttpRequest"
    //     };
    //     var generate = OpenApiContractGenerator.Generate(
    //         stream,
    //         cfg,
    //         e => output.WriteLine(e)
    //     );
    //     output.WriteLine(generate);
    // }
}