namespace Shiny.Mediator.Infrastructure;

public interface IStorageManager
{
    void Store(object request, object result, bool isPeristent);
    
    TResult? Get<TResult>(object request, bool isPeristent);

    void ClearAll();
    // TODO: remove(request) & clearall, clearbyrequesttype
}