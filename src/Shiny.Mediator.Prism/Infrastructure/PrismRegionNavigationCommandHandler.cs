namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismRegionNavigationCommandHandler<TCommand>(
    IDispatcher dispatcher
) : ICommandHandler<TCommand> where TCommand : IPrismRegionNavigationCommand
{
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
