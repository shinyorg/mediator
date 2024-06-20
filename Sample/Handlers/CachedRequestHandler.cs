using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class CachedRequestHandler : IRequestHandler<CachedRequest, string>
{
    [Cache(MaxAgeSeconds = 20)]
    public Task<string> Handle(CachedRequest request, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.UtcNow.ToLocalTime().ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}