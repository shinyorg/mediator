namespace Shiny.Mediator;

public record CacheContext(
    string RequestKey,
    bool IsHit,
    DateTimeOffset Timestamp
);