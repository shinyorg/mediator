using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;

// Mediator
//     Offline Async Enumerable
// Pump current offline data 
// If result takes X seconds+
//     Re-pump data when new result set comes in
public class NewOfflineStreamMiddleware<TRequest, TResult>(
    ILogger<OfflineAvailableRequestMiddleware<TRequest, TResult>> logger,
    IInternetService connectivity, 
    IOfflineService offline,
    IConfiguration configuration
) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(ExecutionContext<TRequest> context, StreamRequestHandlerDelegate<TResult> next)
    {
        // TODO: from replay
        // var store = await offline.Get<TResult>(request);
        // if (store != null)
        //     yield return store.Value;
        //
        // if (!internet.IsAvailable)
        //     await internet.WaitForAvailable(ct).ConfigureAwait(false);
        //
        // var nxt = next().GetAsyncEnumerator(ct);
        // while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
        // {
        //     // TODO: if current is null, remove?
        //     await offline.Set(request, nxt.Current!);
        //     
        //     yield return nxt.Current;
        // }
        throw new NotImplementedException();
    }
}

public class NewOfflineRequestMiddleware<TRequest, TResult>(
    ILogger<OfflineAvailableRequestMiddleware<TRequest, TResult>> logger,
    IInternetService connectivity, 
    IOfflineService offline,
    IConfiguration configuration
) : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
{
    public Task<TResult> Process(ExecutionContext<TRequest> context, RequestHandlerDelegate<TResult> next)
    {
        throw new NotImplementedException();
    }
}