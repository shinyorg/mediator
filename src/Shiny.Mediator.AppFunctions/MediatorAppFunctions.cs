using Microsoft.Extensions.DependencyInjection;
using Shiny.AppFunctions;

namespace Shiny.Mediator;


/// <summary>
/// Runs app function calls through the mediator. Used by <see cref="IAppFunctionRequestHandler{TRequest, TResult}"/>
/// and <see cref="IAppFunctionCommandHandler{TCommand}"/>; call it yourself from a hand-written
/// <see cref="IAppFunctionHandler{TRequest, TResult}"/> when the app function record is not the mediator contract.
/// </summary>
public static class MediatorAppFunctions
{
    /// <summary>
    /// The <see cref="IMediatorContext"/> header holding the <see cref="AppFunctionContext"/> of the call.
    /// </summary>
    public const string AppFunctionContextHeader = "AppFunctions.Context";


    /// <summary>
    /// Sends <paramref name="request"/> through <see cref="IMediator"/> with the <see cref="AppFunctionContext"/>
    /// attached. A <see cref="ValidateException"/> becomes <see cref="AppFunctionErrorCode.InvalidArgument"/>.
    /// </summary>
    public static async Task<TResult> Request<TRequest, TResult>(TRequest request, AppFunctionContext context, CancellationToken cancellationToken)
        where TRequest : IRequest<TResult>
    {
        var mediator = context.Services.GetRequiredService<IMediator>();
        try
        {
            var (_, result) = await mediator
                .Request(request, cancellationToken, x => x.AddHeader(AppFunctionContextHeader, context))
                .ConfigureAwait(false);

            return result;
        }
        catch (ValidateException ex)
        {
            throw ToAppFunctionException(ex);
        }
    }


    /// <summary>
    /// Sends <paramref name="command"/> through <see cref="IMediator"/> with the <see cref="AppFunctionContext"/>
    /// attached. A <see cref="ValidateException"/> becomes <see cref="AppFunctionErrorCode.InvalidArgument"/>.
    /// </summary>
    public static async Task Send<TCommand>(TCommand command, AppFunctionContext context, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var mediator = context.Services.GetRequiredService<IMediator>();
        try
        {
            await mediator
                .Send(command, cancellationToken, x => x.AddHeader(AppFunctionContextHeader, context))
                .ConfigureAwait(false);
        }
        catch (ValidateException ex)
        {
            throw ToAppFunctionException(ex);
        }
    }


    // the message is shown/spoken to the user, so use the validation messages rather than "Validation failed"
    static AppFunctionException ToAppFunctionException(ValidateException ex)
    {
        var messages = ex.Result.Errors.SelectMany(x => x.Value).ToList();
        var message = messages.Count == 0 ? ex.Message : String.Join(" ", messages);
        return new AppFunctionException(AppFunctionErrorCode.InvalidArgument, message, ex);
    }
}
