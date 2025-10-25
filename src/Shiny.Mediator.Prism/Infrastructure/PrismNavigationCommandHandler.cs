namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismNavigationCommandHandler<TCommand>(
    IGlobalNavigationService navigator,
    IDispatcher dispatcher
) : ICommandHandler<TCommand> where TCommand : IPrismNavigationCommand
{
    public Task Handle(TCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        var pn = command.NavigationParameterName ?? command.GetType().Name;
        var nav = command.Navigator ?? navigator;
        var tcs = new TaskCompletionSource();
        
        var navUri = command.PrependedNavigationUri + command.PageUri;
        var navParams = new NavigationParameters();
        
        navParams.Add(pn, command);
        
        if (command.IsModal)
            navParams.Add(KnownNavigationParameters.UseModalNavigation, true);
        
        if (command.IsAnimated != null)
            navParams.Add(KnownNavigationParameters.Animated, command.IsAnimated.Value);
        
        return dispatcher.DispatchAsync(async () =>
        {
            var result = await nav.NavigateAsync(navUri, navParams);
            if (!result.Success)
                throw new InvalidOperationException("Failed to Navigate", result.Exception);
        });
    }
}