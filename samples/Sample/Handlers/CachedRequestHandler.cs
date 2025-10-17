using Sample.Contracts;

namespace Sample.Handlers;


[MediatorSingleton]
public partial class CachedRequestHandler : IRequestHandler<CacheRequest, string>
{
    [Cache(AbsoluteExpirationSeconds = 20)]
    public Task<string> Handle(CacheRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}