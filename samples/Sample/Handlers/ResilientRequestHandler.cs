using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class ResilientRequestHandler : IRequestHandler<ResilientRequest, string>
{
    static bool timeoutRequest;
    
    public async Task<string> Handle(ResilientRequest request, MediatorContext context, CancellationToken cancellationToken)
    {
        if (timeoutRequest)
        {
            await Task.Delay(3000, cancellationToken);
        }

        timeoutRequest = !timeoutRequest;
        return DateTimeOffset.UtcNow.ToString("f");
    }
}