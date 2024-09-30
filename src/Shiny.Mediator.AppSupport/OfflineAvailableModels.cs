namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class OfflineAvailableAttribute : Attribute;

public record OfflineAvailableFlushRequest : IRequest;

public record OfflineAvailableContext(
    string RequestKey,
    DateTimeOffset Timestamp
);