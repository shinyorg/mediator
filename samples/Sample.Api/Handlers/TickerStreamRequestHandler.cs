using System.Runtime.CompilerServices;

namespace Sample.Api.Handlers;

[MediatorScoped]
public class TickerStreamRequestHandler : IStreamRequestHandler<TickerStreamRequest, int>
{
    [MediatorHttpGet("GetTicker", "/ticker")]
    public async IAsyncEnumerable<int> Handle(
        TickerStreamRequest request, 
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (var i = 0; i < request.Count; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(request.IntervalMs), cancellationToken);
            yield return i;
        }
    }
}

[SourceGenerateJsonConverter]
public partial record TickerStreamRequest(int Count, int IntervalMs) : IStreamRequest<int>;

