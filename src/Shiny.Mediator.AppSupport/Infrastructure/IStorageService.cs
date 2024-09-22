namespace Shiny.Mediator.Infrastructure;


// TODO: this needs a "clear by type - replay, offline, etc"
public interface IStorageService
{
    Task Store(object request, object result, bool isPeristent);
    Task<TResult?> Get<TResult>(object request, bool isPeristent);
    Task Clear();
}