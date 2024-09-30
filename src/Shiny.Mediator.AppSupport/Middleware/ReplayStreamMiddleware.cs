using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Replays the last result before requesting a new one
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public class ReplayStreamMiddleware<TRequest, TResult>(
    ILogger<ReplayStreamMiddleware<TRequest, TResult>> logger,
    IStorageService storage,
    IConfiguration configuration
) : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
{
    public IAsyncEnumerable<TResult> Process(
        ExecutionContext<TRequest> context,
        StreamRequestHandlerDelegate<TResult> next 
    )
    {
        if (!this.IsEnabled(context.Request, context.RequestHandler))
            return next();

        if (context.RequestHandler is not IStreamRequestHandler<TRequest, TResult> streamHandler)
            throw new InvalidOperationException("RequestHandler must implement IStreamRequestHandler");
        
        logger.LogDebug("ReplayStream Enabled - {Request}", context.Request);
        return this.Iterate(
            context.Request, 
            streamHandler, 
            next, 
            context.CancellationToken
        );
    }


    protected bool IsEnabled(TRequest request, IRequestHandler requestHandler)
    {
        var section = configuration.GetHandlerSection("ReplayStream", request, requestHandler);
        var enabled = false;
        
        if (section == null)
        {
            enabled = requestHandler.GetHandlerHandleMethodAttribute<TRequest, ReplayStreamAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }

    protected virtual async IAsyncEnumerable<TResult> Iterate(
        TRequest request, 
        IStreamRequestHandler<TRequest, TResult> requestHandler, 
        StreamRequestHandlerDelegate<TResult> next, 
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var store = await storage.Get<TResult>(request);
        if (store != null)
            yield return store;

        var nxt = next().GetAsyncEnumerator(ct);
        while (await nxt.MoveNextAsync() && !ct.IsCancellationRequested)
        {
            // TODO: if current is null, remove?
            await storage.Store(request, nxt.Current!);
            yield return nxt.Current;
        }
    }
}