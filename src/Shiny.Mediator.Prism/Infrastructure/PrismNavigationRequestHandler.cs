namespace Shiny.Mediator.Prism.Infrastructure;


public class PrismNavigationRequestHandler<TRequest>(IGlobalNavigationService navigator) : IRequestHandler<TRequest> where TRequest : IPrismNavigationRequest
{
    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        var pn = request.NavigationParameterName ?? request.GetType().Name;
        var nav = request.Navigator ?? navigator;
        var tcs = new TaskCompletionSource();
        
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var result = await nav.NavigateAsync(request.PageUri, (pn, request));
                if (!result.Success)
                    throw new InvalidOperationException("Failed to Navigate", result.Exception);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        await tcs.Task.ConfigureAwait(false);
    }
}