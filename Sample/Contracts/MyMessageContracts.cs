using Shiny.Mediator.Middleware;

namespace Sample.Contracts;

public record MyMessageRequest(string Arg, bool FireAndForgetEvents) : IRequest<MyMessageResponse>, ICacheItem
{
    public string CacheKey { get; }
};

public record MyMessageResponse(string Response);

public record MyMessageEvent(string Arg, bool FireAndForgetEvents) : IEvent;