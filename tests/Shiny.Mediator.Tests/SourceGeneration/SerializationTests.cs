using System.Text.Json;

namespace Shiny.Mediator.Tests.SourceGeneration;


public class SerializationTests
{
    [Fact]
    public Task Reader_NonJsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApi.EntityLiveDataResponse>(json);
        return Verify(obj);
    }

    [Fact]
    public Task Reader_Generated_JsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApiGenerated.EntityLiveDataResponse>(json);
        return Verify(obj);
    }

    [Fact]
    public Task Writer_NonJsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApi.EntityLiveDataResponse>(json);
        
        var serializedJson = JsonSerializer.Serialize(obj);
        return Verify(serializedJson);
    }
    
    
    [Fact]
    public Task Writer_Generated_JsonConverter()
    {
        var json = File.ReadAllText("./SourceGeneration/serialization.json");
        var obj = JsonSerializer.Deserialize<ThemeParksApiGenerated.EntityLiveDataResponse>(json);
        
        var serializedJson = JsonSerializer.Serialize(obj);
        return Verify(serializedJson);
    }
}


// [SourceGenerateJsonConverter]
// public partial record VehicleResult(int Id, string Manufacturer, string Model)
// {
//     public string Name => $"{Manufacturer} {Model}";
// };
