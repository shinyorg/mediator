using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Mediator;

namespace Sample.Handlers;

public record OfflineRequest : IRequest<string>;

[SingletonHandler]
public class OfflineRequestHandler : IRequestHandler<OfflineRequest, string>
{
    [OfflineAvailable]
    public Task<string> Handle(OfflineRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var r = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.FromResult(r);
    }
}