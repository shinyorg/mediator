namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismNavigationRequestHandler<TRequest>(IGlobalNavigationService navigator) : IRequestHandler<TRequest> where TRequest : IPrismNavigationRequest
{
    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        var pn = request.NavigationParameterName ?? request.GetType().Name;
        var nav = request.Navigator ?? navigator;
        var tcs = new TaskCompletionSource();
        
        var navUri = request.PrependedNavigationUri + request.PageUri;
        var navParams = new NavigationParameters();
        
        navParams.Add(pn, request);
        
        if (request.IsModal)
            navParams.Add(KnownNavigationParameters.UseModalNavigation, true);
        
        if (request.IsAnimated ?? true)
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