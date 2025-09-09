using System.Text.Json.Serialization;
using System.Text.Json;
using Sample.Contracts;

namespace Sample;

public class MyTestClassJsonConverter : JsonConverter<MyTestClass>
{
    public override MyTestClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        var testClass = new MyTestClass();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "id":
                    testClass.Id = reader.GetInt32();
                    break;
                case "value":
                    testClass.Value = reader.GetString() ?? string.Empty;
                    break;
                case "timestamp":
                    if (reader.TokenType != JsonTokenType.Null)
                        testClass.Timestamp = reader.GetDateTimeOffset();
                    break;
                case "parent":
                    if (reader.TokenType != JsonTokenType.Null)
                        testClass.Parent = JsonSerializer.Deserialize<MyTestClass>(ref reader, options);
                    break;
                case "children":
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        var children = JsonSerializer.Deserialize<List<MyTestClass>>(ref reader, options);
                        testClass.Children = children ?? new List<MyTestClass>();
                    }
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return testClass;
    }

    public override void Write(Utf8JsonWriter writer, MyTestClass value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteNumber("id", value.Id);
        writer.WriteString("value", value.Value);
        
        if (value.Timestamp.HasValue)
            writer.WriteString("timestamp", value.Timestamp.Value);
        else
            writer.WriteNull("timestamp");

        if (value.Parent != null)
        {
            writer.WritePropertyName("parent");
            JsonSerializer.Serialize(writer, value.Parent, options);
        }
        else
        {
            writer.WriteNull("parent");
        }

        writer.WritePropertyName("children");
        JsonSerializer.Serialize(writer, value.Children, options);

        writer.WriteEndObject();
    }
}

[JsonConverter(typeof(MyTestClassJsonConverter))]
public class MyTestClass
{
    public int Id { get; set; }
    public string Value { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    
    public MyTestClass? Parent { get; set; }
    public List<MyTestClass> Children { get; set; } = new();
}

[JsonSerializable(typeof(MyTestClass))]
[JsonSerializable(typeof(List<MyTestClass>))]
[JsonSourceGenerationOptions(Converters = [typeof(MyTestClassJsonConverter)])]
public partial class JsonTests : JsonSerializerContext
{
    
}