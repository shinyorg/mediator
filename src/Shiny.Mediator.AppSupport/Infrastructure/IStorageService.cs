namespace Shiny.Mediator.Infrastructure;


public interface IStorageService
{
    Task Store(object request, object result, bool isPeristent);
    Task<TResult?> Get<TResult>(object request, bool isPeristent);
    Task Clear();
}