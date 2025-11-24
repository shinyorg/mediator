using Shiny.Mediator.Http;

namespace Sample;

[Get("/test/request/{Arg}")]
public class MyTestHttpRequest : IRequest<string>
{
    public int? Arg { get; set; }
}

[Post("/test/stream")]
public class MyTestStreamHttpRequest : IStreamRequest<DateTimeOffset>
{
    
}