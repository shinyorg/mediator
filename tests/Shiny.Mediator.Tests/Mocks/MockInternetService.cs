using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;



public class MockInternetService : IInternetService
{
    public bool IsAvailable { get; set; }
    public Task WaitForAvailable(CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}