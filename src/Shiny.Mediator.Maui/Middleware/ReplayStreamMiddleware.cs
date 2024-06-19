using System.Text.Json;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Replays the last result before requesting a new one
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class ReplayStreamMiddleware<TRequest, TResult>(IFileSystem fileSystem) : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerator<TResult> Process(
        TRequest request, 
        StreamRequestDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ReplayAttribute>();
        if (attribute == null)
            return next();

        return this.Iterate(request, next, cancellationToken);
    }


    protected virtual async IAsyncEnumerator<TResult> Iterate(TRequest request, StreamRequestDelegate<TResult> next, CancellationToken ct)
    {
        var path = this.GetCacheFilePath(request);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var obj = JsonSerializer.Deserialize<TResult>(json);
            yield return obj;
        }

        var nxt = next();
        while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
        {
            var json = JsonSerializer.Serialize(nxt.Current);
            File.WriteAllText(path, json);
            yield return nxt.Current;
        }
    }

    protected virtual string GetCacheFilePath(TRequest request)
    {
        var key = this.GetCacheKey(request);
        return Path.Combine(fileSystem.CacheDirectory, $"{key}.replay");
    }


    protected virtual string GetCacheKey(TRequest request)
    {
        if (request is IReplayKey<TResult> key)
            return key.Key;

        var t = request.GetType();
        return $"{t.Namespace}_{t.Name}";
    }
}