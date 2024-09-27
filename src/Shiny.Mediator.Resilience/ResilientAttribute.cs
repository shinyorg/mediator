namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class ResilientAttribute(string configurationKey) : Attribute
{
    public string ConfigurationKey => configurationKey;
}