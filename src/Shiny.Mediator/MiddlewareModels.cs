namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TimerRefreshAttribute(int refreshSeconds) : Attribute
{
    public int RefreshSeconds => refreshSeconds;
}