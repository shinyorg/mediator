namespace Shiny.Mediator.Infrastructure;

public interface IExecutionMiddleware
{
    Action Start(IMediatorContext context);
    Action OnMiddlewareExecute(IMediatorContext context, object middleware);
    Action OnHandlerExecute(IMediatorContext context);
}