using Shiny.Net.Http;

namespace Shiny.Mediator.HttpTransferBroadcast;

/// <summary>
/// Shiny HTTP transfer delegate that forwards background upload and download callbacks into the mediator
/// pipeline as <see cref="HttpTransferEvents"/>. Acts as the bridge between Shiny's <see cref="IHttpTransferDelegate"/>
/// contract and <see cref="IHttpTransferReceiver"/> handlers.
/// </summary>
public class HttpTransferBroadcaster(IMediator mediator) : IHttpTransferDelegate
{
    /// <inheritdoc/>
    public async Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
    }


    /// <inheritdoc/>
    public async Task OnCompleted(HttpTransferRequest request)
    {
    }
}