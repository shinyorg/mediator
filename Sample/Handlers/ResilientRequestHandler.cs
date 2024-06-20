using Sample.Contracts;
using Shiny.Mediator.Resilience;

namespace Sample.Handlers;


[RegisterHandler]
public class ResilientRequestHandler : IRequestHandler<ResilientRequest, string>
{
    static bool timeoutRequest;
    
    [Resilient("test")]
    public async Task<string> Handle(ResilientRequest request, CancellationToken cancellationToken)
    {
        if (timeoutRequest)
        {
            await Task.Delay(3000, cancellationToken);
        }

        timeoutRequest = !timeoutRequest;
        return DateTimeOffset.UtcNow.ToString("f");
    }
}