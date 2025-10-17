namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class OfflineAvailableAttribute : MediatorMiddlewareAttribute;

public record OfflineAvailableContext(
    string RequestKey,
    DateTimeOffset Timestamp
);