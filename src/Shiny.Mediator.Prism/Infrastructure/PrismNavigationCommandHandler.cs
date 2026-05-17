namespace Shiny.Mediator.Prism.Infrastructure;


/// <summary>
/// Open-generic <see cref="ICommandHandler{TCommand}"/> that executes
/// <see cref="IPrismNavigationCommand"/> by composing the navigation URI, populating Prism
/// <c>NavigationParameters</c> (modal/animated flags, command payload), and dispatching the
/// navigation on the UI thread. Falls back to <see cref="IGlobalNavigationService"/> when the
/// command supplies no <see cref="IPrismNavigationCommand.Navigator"/>.
/// </summary>
public class PrismNavigationCommandHandler<TCommand>(
    IGlobalNavigationService navigator,
    IDispatcher dispatcher
) : ICommandHandler<TCommand> where TCommand : IPrismNavigationCommand
{
    /// <inheritdoc/>
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