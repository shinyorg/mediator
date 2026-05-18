namespace Shiny.Mediator.Prism.Infrastructure;


/// <summary>
/// Open-generic <see cref="ICommandHandler{TCommand}"/> that executes
/// <see cref="IPrismRegionNavigationCommand"/> by marshalling to the UI thread and invoking
/// <c>IRegionManager.RequestNavigate</c>.
/// </summary>
public class PrismRegionNavigationCommandHandler<TCommand>(
    IDispatcher dispatcher
) : ICommandHandler<TCommand> where TCommand : IPrismRegionNavigationCommand
{
    /// <inheritdoc/>
    public Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        var pn = command.NavigationParameterName ?? command.GetType().Name;

        var navParams = new NavigationParameters();
        navParams.Add(pn, command);

        return dispatcher.DispatchAsync(() =>
        {
            command.RegionManager.RequestNavigate(command.RegionName, command.ViewName, navParams);
        });
    }
}
