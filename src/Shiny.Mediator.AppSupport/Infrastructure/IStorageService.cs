namespace Shiny.Mediator.Infrastructure;


public interface IStorageService
{
    /// <summary>
    /// Sets an object into the store
    /// </summary>
    /// <param name="category">
    /// Offline, Cache, Other
    /// This allows modules to stay disconnected since Request Keys can be the same
    /// </param>
    /// <param name="key">Request Key</param>
    /// <param name="value">Value to store</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task Set<T>(string category, string key, T value, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets an object by key, returns null if not found
    /// </summary>
    /// <param name="category">
    /// Offline, Cache, Other
    /// This allows modules to stay disconnected since Request Keys can be the same
    /// </param>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> Get<T>(string category, string key, CancellationToken cancellationToken);

    /// <summary>
    /// Performs various checks - pass null for type & prefix to clear all
    /// </summary>
    /// <param name="category">
    /// Offline, Cache, Other
    /// This allows modules to stay disconnected since Request Keys can be the same
    /// </param>
    /// <param name="requestKey"></param>
    /// <param name="partialMatchKey">Performs a "startsWith" if true, otherwise, performs equal match</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Remove(string category, string requestKey, bool partialMatchKey = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all stores for a category
    /// </summary>
    /// <param name="category"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Clear(string category, CancellationToken cancellationToken);
}
