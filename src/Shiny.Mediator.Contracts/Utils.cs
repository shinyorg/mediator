namespace Shiny.Mediator;

public static class Utils
{
    public static TimestampedResult<T> From<T>(T result, DateTimeOffset? dt)
        => new (dt ?? DateTimeOffset.UtcNow, result);
}