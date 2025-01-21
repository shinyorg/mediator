using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class OfflineRequestHandler : IRequestHandler<OfflineRequest, string>
{
    [OfflineAvailable]
    public Task<string> Handle(OfflineRequest request, RequestContext<OfflineRequest> context, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}