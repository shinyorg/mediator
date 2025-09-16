using System.Text.Json.Serialization;

namespace Sample;


[SourceGenerateJsonConverter]
public partial class MyTestClass
{
    public int Id { get; set; }
    public string Value { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    
    public MyTestClass? Parent { get; set; }
    public List<MyTestClass> Children { get; set; } = new();
}


[JsonSerializable(typeof(MyTestClass))]
[JsonSerializable(typeof(List<MyTestClass>))]
public partial class JsonTests : JsonSerializerContext
{
    
}