using System.Reflection;
using System.Text.Json;

namespace Shiny.Mediator.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public class CacheAttribute : Attribute 
{
    // TODO: duration?  configuration?
}


public class ConnectivityCacheRequestMiddleware<TRequest, TResult>(IConnectivity connectivity, IFileSystem fileSystem) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(TRequest request, Func<Task<TResult>> next, CancellationToken cancellationToken)
    {
        var config = typeof(TRequest).GetCustomAttribute<CacheAttribute>();
        if (config == null)
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        var storePath = Path.Combine(fileSystem.CacheDirectory, $"{typeof(TRequest).Name}.cache");
        
        if (connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            result = JsonSerializer.Deserialize<TResult>(storePath);
        }
        else
        {
            result = await next().ConfigureAwait(false);
            var json = JsonSerializer.Serialize(result);
            await File.WriteAllTextAsync(storePath, json, cancellationToken).ConfigureAwait(false);
        }
        return result;
    }
}