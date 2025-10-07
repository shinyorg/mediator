using Sample.Contracts;

namespace Sample.Handlers;


[SingletonMediatorHandler]
public class OfflineRequestHandler : IRequestHandler<OfflineRequest, string>
{
    [OfflineAvailable]
    public Task<string> Handle(OfflineRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}