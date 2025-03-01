namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismNavigationCommandHandler<TCommand>(
    IGlobalNavigationService navigator
) : ICommandHandler<TCommand> where TCommand : IPrismNavigationCommand
{
    public async Task Handle(TCommand command, MediatorContext context, CancellationToken cancellationToken)
    {
        var pn = command.NavigationParameterName ?? command.GetType().Name;
        var nav = command.Navigator ?? navigator;
        var tcs = new TaskCompletionSource();
        
        var navUri = command.PrependedNavigationUri + command.PageUri;
        var navParams = new NavigationParameters();
        
        navParams.Add(pn, command);
        
        if (command.IsModal)
            navParams.Add(KnownNavigationParameters.UseModalNavigation, true);
        
        if (command.IsAnimated ?? true)
            navParams.Add(KnownNavigationParameters.Animated, true);
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var result = await nav.NavigateAsync(navUri, navParams);
                if (!result.Success)
                    throw new InvalidOperationException("Failed to Navigate", result.Exception);
                
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task.ConfigureAwait(false);
    }
}