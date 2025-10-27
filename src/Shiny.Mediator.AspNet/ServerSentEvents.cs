using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Shiny.Mediator;


public static class ServerSentEvents
{
    /// <summary>
    /// Transforms an event stream into server-sent events over HTTP
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="httpContext"></param>
    /// <param name="filter"></param>
    /// <typeparam name="TEvent"></typeparam>
    public static async Task EventStreamToServerSentEvents<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TEvent>(
        this IMediator mediator, 
        IHttpContextAccessor httpContext,
        Func<TEvent, bool>? filter = null
    ) where TEvent : IEvent
    {
        var r = httpContext.HttpContext!.Response;
        var ct = httpContext.HttpContext.RequestAborted;
        var en = mediator.EventStream<TEvent>(filter, ct);
 
        r.OnStarting(() =>
        {
            r.Clear();
            r.ContentType = "text/event-stream";
            return Task.CompletedTask;
        });
        await foreach (var item in en)
        {
            var json = JsonSerializer.Serialize(item);
            await r.WriteAsync($"data: {json}\n\n", ct);
            await r.Body.FlushAsync(ct);
        }
    }


    /// <summary>
    /// Transforms a request stream into server-sent events over HTTP
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request"></param>
    /// <param name="httpContext"></param>
    /// <typeparam name="TResult"></typeparam>
    public static async Task RequestServerSentEvents<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this IMediator mediator, IStreamRequest<TResult> request, IHttpContextAccessor httpContext)
    {
        var r = httpContext.HttpContext!.Response;
        var ct = httpContext.HttpContext.RequestAborted;
        var en = mediator.Request(request, ct);
        
        r.OnStarting(() =>
        {
            r.Clear();
            r.ContentType = "text/event-stream";
            return Task.CompletedTask;
        });
        await foreach (var item in en)
        {
            var json = JsonSerializer.Serialize(item.Result);
            await r.WriteAsync($"data: {json}\n\n", ct);
            await r.Body.FlushAsync(ct);
        }
    }
}