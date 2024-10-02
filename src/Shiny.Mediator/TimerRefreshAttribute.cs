namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TimerRefreshAttribute(int intervalSeconds) : Attribute
{
    public int IntervalSeconds => intervalSeconds;
}