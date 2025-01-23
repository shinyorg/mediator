using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Tests.Mocks;


public class MockOfflineService : IOfflineService
{
    public Task<string> Set(object request, object result)
    {
        throw new NotImplementedException();
    }

    public Task<OfflineResult<TResult>?> Get<TResult>(object request)
    {
        throw new NotImplementedException();
    }

    public Task ClearByType(Type requestType)  => Task.CompletedTask;
    public Task ClearByRequest(object request) => Task.CompletedTask;
    public Task Clear() => Task.CompletedTask;
}