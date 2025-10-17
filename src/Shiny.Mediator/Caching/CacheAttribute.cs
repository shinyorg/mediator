namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class CacheAttribute : MediatorMiddlewareAttribute
{
    public int AbsoluteExpirationSeconds { get; set; }
    public int SlidingExpirationSeconds { get; set; }
}