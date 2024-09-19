namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ReplayAttribute(bool availableAcrossSessions = true) : Attribute
{
    public bool AvailableAcrossSessions => availableAcrossSessions;
}

