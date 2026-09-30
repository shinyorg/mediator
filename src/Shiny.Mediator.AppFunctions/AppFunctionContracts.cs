using Shiny.AppFunctions;

namespace Shiny.Mediator;


/// <summary>
/// A mediator request that is also an app function. Mark the record with <see cref="AppFunctionAttribute"/> and
/// handle it with an <see cref="IAppFunctionRequestHandler{TRequest, TResult}"/> - Siri, Shortcuts, Apple Intelligence
/// and Android agents then run it through the mediator pipeline (middleware, validation, caching, exception handlers).
/// </summary>
/// <remarks>
/// The record must be declared in the app project - Shiny.AppFunctions only scans the project it builds.
/// </remarks>
public interface IAppFunctionRequest<TResult> : IRequest<TResult>, IAppFunction<TResult>;


/// <summary>
/// A mediator command that is also an app function (no result). Mark the record with <see cref="AppFunctionAttribute"/>
/// and handle it with an <see cref="IAppFunctionCommandHandler{TCommand}"/>.
/// </summary>
/// <remarks>
/// The record must be declared in the app project - Shiny.AppFunctions only scans the project it builds.
/// </remarks>
public interface IAppFunctionCommand : ICommand, IAppFunction;


/// <summary>
/// A mediator request handler that Shiny.AppFunctions also picks up as the app function handler. Implement the
/// mediator <see cref="IRequestHandler{TRequest, TResult}.Handle"/> as usual; app function calls are forwarded to
/// <see cref="IMediator.Request{TResult}(IRequest{TResult}, CancellationToken, Action{IMediatorContext}?)"/>
/// so the full mediator pipeline runs. Use <see cref="AppFunctionMediatorContextExtensions.GetAppFunctionContext"/>
/// inside the handler or middleware to reach the <see cref="AppFunctionContext"/>.
/// </summary>
public interface IAppFunctionRequestHandler<TRequest, TResult>
    : IRequestHandler<TRequest, TResult>, IAppFunctionHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>, IAppFunction<TResult>
{
    Task<TResult> IAppFunctionHandler<TRequest, TResult>.Handle(TRequest request, AppFunctionContext context, CancellationToken cancellationToken)
        => MediatorAppFunctions.Request<TRequest, TResult>(request, context, cancellationToken);
}


/// <summary>
/// A mediator command handler that Shiny.AppFunctions also picks up as the app function handler. Implement the
/// mediator <see cref="ICommandHandler{TCommand}.Handle"/> as usual; app function calls are forwarded to
/// <see cref="IMediator.Send{TCommand}"/> so the full mediator pipeline runs.
/// </summary>
public interface IAppFunctionCommandHandler<TCommand>
    : ICommandHandler<TCommand>, IAppFunctionHandler<TCommand>
    where TCommand : ICommand, IAppFunction
{
    Task IAppFunctionHandler<TCommand>.Handle(TCommand command, AppFunctionContext context, CancellationToken cancellationToken)
        => MediatorAppFunctions.Send(command, context, cancellationToken);
}
