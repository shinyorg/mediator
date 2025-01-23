using System.Runtime.CompilerServices;
using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class TickerStreamRequestHandler : IStreamRequestHandler<TickerRequest, string>
{
    public async IAsyncEnumerable<string> Handle(
        TickerRequest request, 
        RequestContext<TickerRequest> context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (var i = 0; i < request.Repeat; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(request.GapSeconds), cancellationToken);
            var value = i * request.Multiplier;
            yield return ($"{i} : {value}");
        }
    }
}