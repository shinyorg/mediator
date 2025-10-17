namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class TimerRefreshAttribute(int intervalSeconds) : MediatorMiddlewareAttribute
{
    public int IntervalSeconds => intervalSeconds;
}