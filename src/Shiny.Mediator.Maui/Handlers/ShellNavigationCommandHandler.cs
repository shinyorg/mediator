namespace Shiny.Mediator.Handlers;


/// <summary>
/// Open-generic <see cref="ICommandHandler{TCommand}"/> that executes
/// <see cref="IShellNavigationCommand"/> by invoking
/// <see cref="Shell.GoToAsync(ShellNavigationState, bool, System.Collections.Generic.IDictionary{string, object})"/>
/// with the command passed as a Shell navigation parameter.
/// </summary>
public class ShellNavigationCommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : IShellNavigationCommand
{
    /// <inheritdoc/>
    public async Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        var pn = command.ParameterName ?? command.GetType().Name;
        var parms = new Dictionary<string, object> { { pn, command } };
        await Shell.Current.GoToAsync(new ShellNavigationState(command.PageUri), command.Animate ?? true, parms);
    }
}