namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class OfflineAvailableAttribute : Attribute;

public record OfflineAvailableFlushRequest : IRequest;


public static class OfflineExtensions
{
    public static DateTimeOffset? OfflineTimestamp(this IRequestContext context)
        => context.TryGetValue<DateTimeOffset>("Offline.Timestamp");
    
    internal static void SetOfflineTimestamp(this IRequestContext context, DateTimeOffset timestamp)
        => context.Add("Offline.Timestamp", timestamp);
}