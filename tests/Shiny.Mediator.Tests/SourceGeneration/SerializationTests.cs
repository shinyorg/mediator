using System.Text.Json;

namespace Shiny.Mediator.Tests.SourceGeneration;


[SourceGenerateJsonConverter]
public partial class MyTestClass
{
    public Guid? GuidValue { get; set; }
    public string StringValue { get; set; }
    
    public int? IntValue { get; set; }
    
    public DateTimeOffset? DateValue { get; set; }
    
    public List<MyTestClass> Children { get; set; } = new();
}

public class SerializationTests
{
    [Fact]
    public Task Serialize()
    {
        var obj = new MyTestClass
        {
            GuidValue = new Guid("5ae5016e-d35b-4846-91f2-2ff1cace4b97"),
            StringValue = "Test",
            DateValue = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Children = [
                new()
                {
                    IntValue = 2
                }
            ]
        };
        
        var json = JsonSerializer.Serialize(obj);

        return Verify(json).DontScrubDateTimes();
    }


    [Fact]
    public Task Deserialize()
    {
        var obj = JsonSerializer.Deserialize<MyTestClass>(
            """
            {
                 "DateValue": "2015-11-19T00:00:00"
            }
            """);

        return Verify(obj).DontScrubDateTimes();
    }
}