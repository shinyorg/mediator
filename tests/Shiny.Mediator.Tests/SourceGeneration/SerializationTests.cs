using System.Text.Json;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class SerializationTests
{

    [Fact]
    public Task Reader()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<EntityLiveDataResponse>(json);
        return Verify(obj);
    }


    [Fact]
    public Task Writer()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<EntityLiveDataResponse>(json);
        
        var serializedJson = JsonSerializer.Serialize(obj);
        return Verify(serializedJson);
    }
}

[SourceGenerateJsonConverter]
public partial class EntityLiveDataResponse
{
    [global::System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("entityType")]
    public EntityType EntityType { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("timezone")]
    public string Timezone { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("liveData")]
    public List<EntityLiveData> LiveData { get; set; }

}

[SourceGenerateJsonConverter]
public partial class EntityLiveData
{
    [global::System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [global::System.Text.Json.Serialization.JsonPropertyName("entityType")]
    public EntityType EntityType { get; set; }

    // [global::System.Text.Json.Serialization.JsonPropertyName("lastUpdated")]
    // public System.DateTimeOffset LastUpdated { get; set; }

    // [global::System.Text.Json.Serialization.JsonPropertyName("queue")]
    // public global::ShinyWonderland.ThemeParksApi.LiveQueue Queue { get; set; }
    //
    // [global::System.Text.Json.Serialization.JsonPropertyName("showtimes")]
    // public global::System.Collections.Generic.List<global::ShinyWonderland.ThemeParksApi.LiveShowTime> Showtimes { get; set; }
    //
    // [global::System.Text.Json.Serialization.JsonPropertyName("operatingHours")]
    // public global::System.Collections.Generic.List<global::ShinyWonderland.ThemeParksApi.LiveShowTime> OperatingHours { get; set; }
    //
    // [global::System.Text.Json.Serialization.JsonPropertyName("diningAvailability")]
    // public global::System.Collections.Generic.List<global::ShinyWonderland.ThemeParksApi.DiningAvailability> DiningAvailability { get; set; }

}

public enum EntityType
{
    DESTINATION,
    PARK,
    ATTRACTION,
    RESTAURANT,
    HOTEL,
    SHOW,
}