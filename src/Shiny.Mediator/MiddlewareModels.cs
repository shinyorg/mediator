namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class TimerRefreshAttribute(int refreshSeconds, bool ignoreErrors = true) : Attribute
{
    public int RefreshSeconds => refreshSeconds;
    public bool IgnoreErrors => ignoreErrors;
}


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ReplayAttribute : Attribute {}

public interface IReplayKey<TResult> : IStreamRequest<TResult>
{
    string Key { get; }
}