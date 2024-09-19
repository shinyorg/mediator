using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Blazor.Infrastructure;


public class StorageService : IStorageService
{
    public Task Store(object request, object result, bool isPeristent)
    {
        throw new NotImplementedException();
    }

    public Task<TResult?> Get<TResult>(object request, bool isPeristent)
    {
        throw new NotImplementedException();
    }

    public Task Clear()
    {
        throw new NotImplementedException();
    }
}