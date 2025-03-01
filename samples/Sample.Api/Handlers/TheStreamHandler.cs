using System.Runtime.CompilerServices;

namespace Sample.Api.Handlers;

public record TestStreamRequest(int SecondsBetween) : IStreamRequest<string>;

// swagger does not work well with async enumerables
[ScopedHandler]
[MediatorHttpPost("GetStream", "/stream")]
public class TheStreamHandler(ILogger<TheStreamHandler> logger) : IStreamRequestHandler<TestStreamRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        TestStreamRequest request, 
        MediatorContext context, 
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var value = DateTimeOffset.Now.ToString("h:mm:ss tt");
            logger.LogInformation("Returning Value: " + value);
            yield return value;
            await Task.Delay(request.SecondsBetween * 1000, cancellationToken);
        }
        logger.LogInformation("End of stream requested");
    }
}