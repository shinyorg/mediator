// using Shiny.Mediator.SourceGenerators.Http;
// using Xunit.Abstractions;
//
// namespace Shiny.Mediator.Tests;
//
//
// public class HttpRequestGeneratorTests(ITestOutputHelper output)
// {
//     [Fact]
//     public void Tests()
//     { 
//         this.Write("./OpenApi/runtimeConfigurationApiV1.json", "RuntimeConfigApi");
//         this.Write("./OpenApi/subscriptionManagementApiV1.json", "SubMgmtApi");
//         this.Write("./OpenApi/mapsApiV1.json", "MapsApi");
//         this.Write("./OpenApi/notificationApiV1.json", "NotificationsApi");
//         this.Write("./OpenApi/weatherApiV1.json", "WeatherApi");
//         this.Write("./OpenApi/gamePlanApiV1.json", "GamePlanApi");
//         this.Write("./OpenApi/consumerApiV1.json", "ConsumerApi");
//     }
//
//
//     void Write(string readPath, string nameSpace)
//     {
//         using var doc = File.OpenRead(readPath);
//         var item = new MediatorHttpConfig { Namespace = nameSpace };
//         var code = OpenApiContractGenerator.Generate(doc, item, e => output.WriteLine(e));
//         File.WriteAllText(Path.Combine("./Contracts", nameSpace + ".generated.cs"), code);
//     }
// }