using Sample.Contracts;

namespace Sample.Handlers;


[RegisterHandler]
public class OfflineRequestHandler : IRequestHandler<OfflineRequest, string>
{
    [OfflineAvailable(true)]
    public Task<string> Handle(OfflineRequest request, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}