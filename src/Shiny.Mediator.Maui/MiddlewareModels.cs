// [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Shiny.Mediator.Tests")]
namespace Shiny.Mediator;


public enum StoreType
{
    File,
    Memory
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CacheAttribute : Attribute
{
    /// <summary>
    /// Setting this to true will tell the middleware to always passthrough to the server if the app detects online connectivity
    /// False will follow the standard expiration
    /// </summary>
    public bool OnlyForOffline { get; set; }
    
    /// <summary>
    /// Max age of a cache item 
    /// </summary>
    public int MaxAgeSeconds { get; set; }
    
    /// <summary>
    /// You can store in-memory which means the data is not available in subsequent starts OR file where it available across
    /// application sessions
    /// </summary>
    public StoreType Storage { get; set; }
}


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class MainThreadAttribute : Attribute {}


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TimedLoggingAttribute(double errorThresholdMillis) : Attribute
{
    public double ErrorThresholdMillis => errorThresholdMillis;
}


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ReplayAttribute : Attribute {}

public interface IReplayKey<TResult> : IStreamRequest<TResult>
{
    string Key { get; }
}


public record FlushAllCacheRequest : IRequest;
public record FlushCacheItemRequest(object Request) : IRequest;

public class UserExceptionRequestMiddlewareConfig
{
    public bool ShowFullException { get; set; }
    public string ErrorMessage { get; set; } = "We're sorry. An error has occurred";
    public string ErrorTitle { get; set; } = "Error";
    public string ErrorConfirm { get; set; } = "OK";
}