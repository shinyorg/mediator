using Shiny.Net.Http;

namespace Shiny.Mediator.HttpTransferBroadcast;

public class HttpTransferBroadcaster(IMediator mediator) : IHttpTransferDelegate
{
    public async Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
    }


    public async Task OnCompleted(HttpTransferRequest request)
    {
    }
}