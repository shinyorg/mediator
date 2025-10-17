using System.Runtime.CompilerServices;
using Sample.Contracts;

namespace Sample.Handlers;

[MediatorSingleton]
public partial class AutoRefreshStreamRequestHandler : IStreamRequestHandler<AutoRefreshRequest, string>
{
    [TimerRefresh(3000)]
    public async IAsyncEnumerable<string> Handle(
        AutoRefreshRequest request,
        IMediatorContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        yield return DateTimeOffset.Now.ToString("h:mm:ss tt");
    }
}