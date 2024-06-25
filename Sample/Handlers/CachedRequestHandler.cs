using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class CachedRequestHandler : IRequestHandler<OfflineRequest, string>
{
    [OfflineAvailable]
    public Task<string> Handle(OfflineRequest request, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.UtcNow.ToLocalTime().ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}