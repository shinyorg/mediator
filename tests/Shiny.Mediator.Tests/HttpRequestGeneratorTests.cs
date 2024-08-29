using Shiny.Mediator.HttpRequestGenerator;

namespace Shiny.Mediator.Tests;

public class HttpRequestGeneratorTests
{
    [Fact]
    public void Tests()
    {
        var gen = new ContractGenerator();
        gen.Test(File.OpenRead("./OpenApi/weatherApiV1.json"), "./Contracts", "My.Tests", "WeatherApi");
    }
}