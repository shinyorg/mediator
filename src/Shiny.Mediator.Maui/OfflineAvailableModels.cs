namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class OfflineAvailableAttribute(bool availableAcrossSessions = true) : Attribute
{
    public bool AvailableAcrossSessions => availableAcrossSessions;
}


public record OfflineAvailableFlushRequest : IRequest;