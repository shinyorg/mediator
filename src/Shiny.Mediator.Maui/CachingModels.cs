namespace Shiny.Mediator;

public enum StoreType
{
    File,
    Memory
}

/// <summary>
/// Implementing this interface will allow you to create your own cache key, otherwise the cache key is based on the name
/// of the request model
/// </summary>
public interface ICacheKeyProvider<TRequest, TResult> : IRequestHandler<TRequest, TResult> where TRequest :  IRequest<TResult>
{
    string GetCacheKey(TRequest request);
}


[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class CacheAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public string? Category { get; set; }
    
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

public record FlushAllCacheRequest : IRequest;
public record FlushCacheItemRequest(object Request) : IRequest;

public class UserExceptionRequestMiddlewareConfig
{
    public bool ShowFullException { get; set; }
    public string ErrorMessage { get; set; } = "We're sorry. An error has occurred";
    public string ErrorTitle { get; set; } = "Error";
    public string ErrorConfirm { get; set; } = "OK";
}