namespace Shiny.Mediator.Infrastructure;

public class SentryExecutionMiddleware(IHub hub) : IExecutionMiddleware
{
    ITransactionTracer? transaction;
    
    public Action Start(IMediatorContext context)
    {
        this.transaction = hub.StartTransaction("shiny", "mediator");
        return () => this.transaction.Finish();
    }
    

    public Action OnMiddlewareExecute(IMediatorContext context, object middleware)
    {
        var span = this.transaction!.StartChild(middleware.GetType().FullName!);
        return () => span.Finish();
    }

    
    public Action OnHandlerExecute(IMediatorContext context)
    {
        var span = this.transaction!.StartChild("handler");
        return () => span.Finish();
    }
}