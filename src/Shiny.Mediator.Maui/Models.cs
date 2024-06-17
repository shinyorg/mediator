namespace Shiny.Mediator;

public record FlushAllCacheRequest : IRequest;
public record FlushCacheItemRequest(object Request) : IRequest;