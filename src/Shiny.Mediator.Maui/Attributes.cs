namespace Shiny.Mediator;

public enum StoreType
{
    File,
    Memory
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
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
public class TimedLoggingAttribute : Attribute
{
    public double ErrorThresholdMillis { get; set; } = 0;
}


public class UserExceptionRequestMiddlewareConfig
{
    public bool ShowFullException { get; set; }
    public string ErrorMessage { get; set; } = "We're sorry. An error has occurred";
    public string ErrorTitle { get; set; } = "Error";
    public string ErrorConfirm { get; set; } = "OK";
}