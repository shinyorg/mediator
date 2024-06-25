using System.Text.Json;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


public class OfflineAvailableRequestMiddleware<TRequest, TResult>(IConnectivity connectivity, IFileSystem fileSystem) : IRequestMiddleware<TRequest, TResult>
{
    readonly Dictionary<string, object> memCache = new();
    
    public async Task<TResult> Process(
        TRequest request, 
        RequestHandlerDelegate<TResult> next, 
        IRequestHandler requestHandler,
        CancellationToken cancellationToken
    )
    {
        if (typeof(TResult) == typeof(Unit))
            return await next().ConfigureAwait(false);

        var cfg = requestHandler.GetHandlerHandleMethodAttribute<TRequest, OfflineAvailableAttribute>();
        if (cfg == null)
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        var connected = connectivity.NetworkAccess == NetworkAccess.Internet;
        if (connected)
        {
            result = await next().ConfigureAwait(false);
            if (result != null)
                this.Store(request!, result, cfg.AvailableAcrossSessions);
        }
        else
        {
            result = this.GetFromStore(request, cfg.AvailableAcrossSessions);
        }
        return result;
    }
    
    protected virtual string GetStoreKey(object request)
        => Utils.GetRequestKey(request);
    
    
    protected virtual string GetFilePath(object request)
    {
        var key = this.GetStoreKey(request);
        var path = Path.Combine(fileSystem.CacheDirectory, $"{key}.off");
        return path;
    }


    protected virtual void Store(object request, object result, bool acrossSessions)
    {
        if (acrossSessions)
        {
            var path = this.GetFilePath(request);
            var json = JsonSerializer.Serialize(result);
            File.WriteAllText(path, json); 
        }
        else
        {
            var key = this.GetStoreKey(request);
            lock (this.memCache)
                this.memCache[key] = result!;
        }
    }


    protected virtual TResult? GetFromStore(object request, bool acrossSessions)
    {
        TResult? returnValue = default;

        if (acrossSessions)
        {
            var path = this.GetFilePath(request);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var obj = JsonSerializer.Deserialize<TResult>(json)!;
                returnValue = obj;
            }
        }
        else 
        {
            var key = this.GetStoreKey(request);
            lock (this.memCache)
            {
                if (this.memCache.ContainsKey(key))
                {
                    var item = this.memCache[key];
                    returnValue = (TResult)item;
                }
            }
        }
        return returnValue;
    }  
}