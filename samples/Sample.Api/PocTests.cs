using Shiny.Mediator;
using Shiny.Mediator.Http;

namespace Sample.Api;


// route parameter name must exist as class property or ERROR
// querystring is also allowed and follows the same rules.  ex. /poc/test?RouteParameter=5
// Users don't implement mediator interfaces or we error in source gen do it?

// this is for manual/user code implementations - http client generations will remove all parameters and just generate the handlers
// this class must be partial or source gen will error - this allows us to control if IRequest<TResponse> or IStreamRequest<TResponse> is used based on attribute HttpStreamType
// note: there is a GetAttribute, PostAttribute, DeleteAttribute, PutAttribute, PatchAttribute which decide on what HTTP verb to use in source gen
[Get("/poc/test/{RouteParameter}?Query={QueryParameter}")]
public class PocTests : IRequest<ResultObject>
{
    // if null, replace with empty
    // all parameters are used with ToString()
    public int RouteParameter { get; set; }
    
    // if null, replace with empty
    // all parameters are used with ToString()
    public string QueryParameter { get; set; } = string.Empty;
    
    // these values can be any type, we'll always call ToString() on them
    // if constructor not set, property name is used - in this case, X-Custom-Header will be used
    [Header("X-Custom-Header")] 
    public string TestValue { get; set; } = string.Empty;
    
    // these values can be any type, we'll always call ToString() on them
    // header key will be property name - Test2Value
    [Header]
    public string? Test2Value { get; set; }
    
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

