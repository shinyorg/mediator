using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class TickerStreamRequestHandler : IStreamRequestHandler<TickerRequest, string>
{
    public async IAsyncEnumerable<string> Handle(TickerRequest request, CancellationToken cancellationToken)
    {
        for (var i = 0; i < request.Repeat; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(request.GapSeconds));
            var value = i * request.Multiplier;
            yield return ($"{i} : {value}");
        }
    }
}