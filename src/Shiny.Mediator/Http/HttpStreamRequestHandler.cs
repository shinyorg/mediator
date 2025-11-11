using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Http;




public class HttpStreamRequestHandler<TRequest, TResult>(
    ILogger<HttpStreamRequestHandler<TRequest, TResult>> logger,
    IConfiguration configuration,
    ISerializerService serializer,
    IHttpClientFactory httpClientFactory,
    IEnumerable<IHttpRequestDecorator> decorators
) : BaseHttpRequestHandler(
    logger,
    configuration,
    serializer,
    httpClientFactory,
    decorators
), IStreamRequestHandler<TRequest, TResult> where TRequest : IHttpStreamRequest<TResult>
{
    //[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "GetValue will not be trimmed")]
    public async IAsyncEnumerable<TResult> Handle(TRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        //var httpRequest = await this.BuildRequest(request, context, cancellationToken).ConfigureAwait(false);
        var httpClient = httpClientFactory.CreateClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "http://example.com/stream"); // Placeholder
        
        var httpResponse = await httpClient
            .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
       
        // context.SetHttp(httpRequest, httpResponse);
        // await this.WriteDebugIfEnable(httpRequest, httpResponse, cancellationToken).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
       
        await using var responseStream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);

        var st = HttpStreamType.PlainStream;
        if (request.StreamType == null)
        {
            st = httpResponse.Content.Headers.ContentType?.MediaType?.Equals("text/event-stream", StringComparison.InvariantCultureIgnoreCase) ?? false
                ? HttpStreamType.ServerSentEvents
                : HttpStreamType.PlainStream;
        }

        if (st == HttpStreamType.PlainStream)
        {
            await foreach (var obj in serializer.DeserlializeAsyncEnumerable<TResult>(responseStream, cancellationToken))
            { 
                yield return obj;
            }
        }
        else
        {
            await foreach (var sseEvent in ReadServerSentEvents<TResult>(responseStream, cancellationToken))
            {
                yield return sseEvent;
            }
        }
    }
    
    
    async IAsyncEnumerable<T> ReadServerSentEvents<T>(
        Stream stream, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var sb = new StringBuilder();
   
        while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (String.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0)
                {
                    var json = sb.ToString();
                    sb.Clear();
                    yield return serializer.Deserialize<T>(json);
                }
            }
            else if (line.StartsWith("data:"))
            {
                sb.AppendLine(line.Substring("data:".Length).Trim());
            }
        }
        // Flush any remaining data
        // if (sb.Length > 0)
        // {
        //     yield return sb.ToString();
        // }
    }
}