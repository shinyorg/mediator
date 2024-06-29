namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ReplayAttribute(bool availableAcrossSessions = true) : Attribute
{
    public bool AvailableAcrossSessions => availableAcrossSessions;
}

