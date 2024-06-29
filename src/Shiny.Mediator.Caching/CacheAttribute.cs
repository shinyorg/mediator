using Microsoft.Extensions.Caching.Memory;

namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class CacheAttribute : Attribute
{
    public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
    public int AbsoluteExpirationSeconds { get; set; }
    public int SlidingExpirationSeconds { get; set; }
}