using Shiny.Mediator.HttpRequestGenerator;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class HttpRequestGeneratorTests(ITestOutputHelper output)
{
    [Fact]
    public void Tests()
    {
        var gen = new ContractGenerator();
        
        gen.Test(File.OpenRead("./OpenApi/runtimeConfigurationApiV1.json"), "./Contracts", "RuntimeConfigApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/subscriptionManagementApiV1.json"), "./Contracts", "SubMgmtApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/mapsApiV1.json"), "./Contracts", "MapsApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/notificationApiV1.json"), "./Contracts", "NotificationsApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/weatherApiV1.json"), "./Contracts", "WeatherApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/gamePlanApiV1.json"), "./Contracts", "GamePlanApi", output.WriteLine);
        gen.Test(File.OpenRead("./OpenApi/consumerApiV1.json"), "./Contracts", "ConsumerApi", output.WriteLine);
    }
}