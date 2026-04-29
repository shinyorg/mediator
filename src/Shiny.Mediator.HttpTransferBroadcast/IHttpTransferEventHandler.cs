using Shiny.Net.Http;

namespace Shiny.Mediator.HttpTransferBroadcast;

public record HttpTransferEvents(HttpTransfer[] Transfers) : IEvent;
public interface IHttpTransferReceiver : IEventHandler<HttpTransferEvents>;