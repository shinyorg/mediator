namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method)]
public class ResilientAttribute(string configurationKey) : MediatorMiddlewareAttribute
{
    public string ConfigurationKey => configurationKey;
}