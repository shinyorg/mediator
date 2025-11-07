namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismRegionNavigationCommandHandler<TCommand>(
    IRegionManager regionManager,
    IDispatcher dispatcher
) : ICommandHandler<TCommand> where TCommand : IPrismRegionNavigationCommand
{
    public Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        var pn = command.NavigationParameterName ?? command.GetType().Name;
        var rm = command.RegionManager ?? regionManager;

        var navParams = new NavigationParameters();
        navParams.Add(pn, command);

        return dispatcher.DispatchAsync(() =>
        {
            rm.RequestNavigate(command.RegionName, command.ViewName, navParams);
        });
    }
}
