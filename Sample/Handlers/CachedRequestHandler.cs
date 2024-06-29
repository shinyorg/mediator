using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class CachedRequestHandler : IRequestHandler<CacheRequest, string>
{
    [Cache(AbsoluteExpirationSeconds = 20)]
    public Task<string> Handle(CacheRequest request, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}