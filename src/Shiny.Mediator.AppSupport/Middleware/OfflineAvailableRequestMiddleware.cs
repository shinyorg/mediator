using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Middleware;


/// <summary>
/// Request middleware that returns the last stored result when the device is offline or the handler times out,
/// and refreshes the offline store whenever the handler executes successfully. Activated by
/// <see cref="OfflineAvailableAttribute"/> on the handler method or by an <c>Offline</c> configuration section.
/// </summary>
public class OfflineAvailableRequestMiddleware<TRequest, TResult>(
    IInternetService connectivity,
    IOfflineService offline,
    ILogger<OfflineAvailableRequestMiddleware<TRequest, TResult>>? logger = null,
    IConfiguration? configuration = null
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    /// <inheritdoc/>
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken
    )
    {
        if (!this.IsEnabled(context))
            return await next().ConfigureAwait(false);
        
        var result = default(TResult);
        if (connectivity.IsAvailable)
        {
            try
            {
                result = await next().ConfigureAwait(false);
                var requestKey = await offline.Set(context.Message!, result!, cancellationToken);
                logger?.LogDebug("Offline: {Request} - Key: {RequestKey}", context.Message, requestKey);
            }
            catch (TimeoutException)
            {
                result = await this.GetOffline(context, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            result = await this.GetOffline(context, cancellationToken).ConfigureAwait(false);
        }
        return result!;
    }


    async Task<TResult?> GetOffline(IMediatorContext context, CancellationToken cancellationToken)
    {
        TResult result = default!;
        var offlineResult = await offline
            .Get<TResult>(context.Message!, cancellationToken)
            .ConfigureAwait(false);
            
        if (offlineResult != null)
        {
            context.Offline(new OfflineAvailableContext(offlineResult.RequestKey, offlineResult.Timestamp));
            result = offlineResult.Value;
            
            logger?.LogDebug(
                "Offline Hit: {Request} - Timestamp: {Timestamp} - Key: {RequestKey}", 
                context.Message, 
                offlineResult.Timestamp,
                offlineResult.RequestKey
            );
        }

        return result;
    }

    
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Get will not be trimmed")]
    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Get will not be trimmed")]
    bool IsEnabled(IMediatorContext context)
    {
        var enabled = false;
        var section = context.GetHandlerSection(configuration, "Offline");
        if (section == null)
        {
            enabled = context.GetHandlerAttribute<OfflineAvailableAttribute>() != null;
        }
        else
        {
            enabled = section.Get<bool>();
        }
        return enabled;
    }
}