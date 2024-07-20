namespace Shiny.Mediator;

public record TimestampedResult<T>(DateTimeOffset Timestamp, T Value);
