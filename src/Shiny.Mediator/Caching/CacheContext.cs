using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Caching;

public record CacheContext(
    string RequestKey,
    bool IsHit,
    DateTimeOffset Timestamp,
    CacheItemConfig? Config = null
);