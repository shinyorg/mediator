using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Http;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class HttpDirectRequestHandlerTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(
        "Mediator:Http:Direct:Test:Url", 
        "https://api.themeparks.wiki/v1/entity/66f5d97a-a530-40bf-a712-a6317c96b06d", 
        "Test"
    )]
    [InlineData(
        "Mediator:Http:Direct:BaseUrl", 
        "https://api.themeparks.wiki/v1", 
        "/entity/66f5d97a-a530-40bf-a712-a6317c96b06d"
    )]
    public async Task e2e(string key, string value, string configOrRoute)
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        services.AddConfiguration(cfg =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string>
            {
                { key, value }
            });
        });
        services.AddShinyMediator(cfg =>
        {
            cfg.AddHttpClient();
        }, false);
        
        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var request = new HttpDirectRequest
        {
            ConfigNameOrRoute = configOrRoute,
            ResultType = typeof(EntityInfo)
        };
        
        var result = await mediator.Request(request);
        await Verify(result).UseParameters(key, value, configOrRoute);
    }
}

file class EntityInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("location")]
    public Location Location { get; set; }

    [JsonPropertyName("parentId")]
    public string ParentId { get; set; }
    
    [JsonPropertyName("timezone")]
    public string TimeZone { get; set; }
    
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; }

    [JsonPropertyName("destinationId")]
    public string DestinationId { get; set; }
    
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; }
}

file class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}