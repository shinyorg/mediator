namespace Shiny.Mediator.Handlers;


public class ShellNavigationRequestHandler<TRequest> : IRequestHandler<TRequest> where TRequest : IShellNavigationRequest
{
    public async Task Handle(TRequest request, CancellationToken cancellationToken)
    {
        var pn = request.ParameterName ?? request.GetType().Name;
        var parms = new Dictionary<string, object> { { pn, request } };
        await Shell.Current.GoToAsync(new ShellNavigationState(request.PageUri), request.Animate ?? true, parms);
    }
}