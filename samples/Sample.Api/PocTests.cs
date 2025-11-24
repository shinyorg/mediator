using Shiny.Mediator;
using Shiny.Mediator.Http;

namespace Sample.Api;


// route parameter name must exist as class property else the source generator should report diagnostic error
// note: there is a GetAttribute, PostAttribute, DeleteAttribute, PutAttribute, PatchAttribute which decide on what HTTP verb to use in source gen
[Get("/poc/test/{RouteParameter}")]
public class PocTests : IRequest<ResultObject>
{
    // if null, replace with empty
    // all parameters are used with ToString()
    public int RouteParameter { get; set; }
    
    // these values can be any type, we'll always call ToString() on them
    // if constructor not set, property name is used - in this case, X-Custom-Header will be used
    [Header("X-Custom-Header")] 
    public string TestValue { get; set; } = string.Empty;
    
    // these values can be any type, we'll always call ToString() on them
    // header key will be property name - Test2Value
    [Header]
    public string? Test2Value { get; set; }

    // query works the same as header, if value is null, use it
    // if constructor arg in attribute is not set, use property name
    // the values can be any type, we'll always call ToString on them
    [Query("TestQuery")]
    public int? QueryValue { get; set; }

    // only one body allowed per object or error
    // if value is null, no body is sent
    [Body]
    public TestBody? Body { get; set; }
}

public class TestBody
{
    public string Hello { get; set; } = string.Empty;
}

public class ResultObject
{
    public string World { get; set; } = string.Empty;
}

[Get("/poc/stream/")]
public class PocStreamRequest : IStreamRequest<ResultObject>;

