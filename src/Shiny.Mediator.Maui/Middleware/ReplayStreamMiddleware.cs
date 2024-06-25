using System.Runtime.CompilerServices;
using System.Text.Json;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Replays the last result before requesting a new one
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class ReplayStreamMiddleware<TRequest, TResult>(IFileSystem fileSystem) : IStreamRequestMiddleware<TRequest, TResult> 
    where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        TRequest request, 
        StreamRequestHandlerDelegate<TResult> next, 
        IStreamRequestHandler<TRequest, TResult> requestHandler,
        CancellationToken cancellationToken
    )
    {
        var attribute = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ReplayAttribute>();
        if (attribute == null)
            return next();

        return this.Iterate(request, requestHandler, next, cancellationToken);
    }


    protected virtual async IAsyncEnumerable<TResult> Iterate(
        TRequest request, 
        IStreamRequestHandler<TRequest, TResult> requestHandler, 
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var path = this.GetCacheFilePath(request);
        if (File.Exists(path))
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var obj = JsonSerializer.Deserialize<TResult>(json);
            yield return obj!;
        }

        var nxt = next().GetAsyncEnumerator(ct);
        while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
        {
            var json = JsonSerializer.Serialize(nxt.Current);
            await File.WriteAllTextAsync(path, json, ct);
            yield return nxt.Current;
        }
    }

    protected virtual string GetCacheFilePath(TRequest request)
    {
        var key = this.GetCacheKey(request);
        return Path.Combine(fileSystem.CacheDirectory, $"{key}.replay");
    }


    protected virtual string GetCacheKey(TRequest request)
        => Utils.GetRequestKey(request);
}