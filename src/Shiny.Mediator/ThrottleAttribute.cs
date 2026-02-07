namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ThrottleAttribute(long milliseconds) : MediatorMiddlewareAttribute
{
    public long Milliseconds => milliseconds;
}
