namespace Shiny.Mediator.Handlers;


public class ShellNavigationCommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : IShellNavigationCommand
{
    public async Task Handle(TCommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var pn = command.ParameterName ?? command.GetType().Name;
        var parms = new Dictionary<string, object> { { pn, command } };
        await Shell.Current.GoToAsync(new ShellNavigationState(command.PageUri), command.Animate ?? true, parms);
    }
}