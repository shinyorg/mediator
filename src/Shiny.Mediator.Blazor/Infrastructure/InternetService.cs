using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;

public class InternetService : IInternetService
{
    public bool IsAvailable { get; }
    public Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}