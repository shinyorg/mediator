using Shiny.Net.Http;

namespace Shiny.Mediator.HttpTransferBroadcast;

/// <summary>
/// Mediator event published when Shiny HTTP background transfers change state (progress, completion, or failure).
/// Subscribers receive the affected <see cref="HttpTransfer"/> entries forwarded from the Shiny HTTP transfer infrastructure.
/// </summary>
/// <param name="Transfers">The HTTP transfers included in this broadcast.</param>
public record HttpTransferEvents(HttpTransfer[] Transfers) : IEvent;

/// <summary>
/// Marker handler interface for components that react to <see cref="HttpTransferEvents"/> published by the
/// HTTP transfer broadcaster. Implement and register this interface to observe background upload and download
/// activity through the mediator pipeline.
/// </summary>
public interface IHttpTransferReceiver : IEventHandler<HttpTransferEvents>;