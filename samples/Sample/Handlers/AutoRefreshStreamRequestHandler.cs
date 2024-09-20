using System.Runtime.CompilerServices;
using Sample.Contracts;

namespace Sample.Handlers;

[SingletonHandler]
public class AutoRefreshStreamRequestHandler : IStreamRequestHandler<AutoRefreshRequest, string>
{
    [TimerRefresh(3000)]
    public async IAsyncEnumerable<string> Handle(AutoRefreshRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return DateTimeOffset.Now.ToString("h:mm:ss tt");
    }
}