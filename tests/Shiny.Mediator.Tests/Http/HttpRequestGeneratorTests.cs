using Shiny.Mediator.SourceGenerators.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpRequestGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void Tests()
    { 
        this.Write("./Http/test.json", "ConsumerApi");
    }


    void Write(string readPath, string nameSpace)
    {
        using var doc = File.OpenRead(readPath);
        var item = new MediatorHttpItemConfig { Namespace = nameSpace };
        var code = OpenApiContractGenerator.Generate(doc, item, e => output.WriteLine(e));
        
        Console.Write(code);
        // File.WriteAllText(Path.Combine("./Contracts", nameSpace + ".generated.cs"), code);
    }
}