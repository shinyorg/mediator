namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class MiddlewareOrderAttribute(int order) : Attribute
{
    public int Order => order;
}
