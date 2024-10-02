namespace Shiny.Mediator.Infrastructure;


public interface IStorageService
{
    Task Set<T>(string key, T value);
    Task<T> Get<T>(string key);
    Task Remove(string key);
}