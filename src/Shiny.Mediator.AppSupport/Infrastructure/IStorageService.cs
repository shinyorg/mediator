namespace Shiny.Mediator.Infrastructure;


public interface IStorageService
{
    Task Store(object request, object result);
    Task<TResult?> Get<TResult>(object request);
    // Task ClearByRequestKey(IRequestKey requestKey)
    // Task ClearByType(Type requestType);
    Task Clear();
}