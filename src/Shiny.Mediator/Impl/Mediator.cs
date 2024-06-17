using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class Mediator(
    IRequestSender requestSender,
    IEventPublisher eventPublisher
) : IMediator
{
    public Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
        => requestSender.Request(request, cancellationToken);

    public Task Send(IRequest request, CancellationToken cancellationToken = default)
        => requestSender.Send(request, cancellationToken);

    public IAsyncEnumerable<TResult> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
        => requestSender.Request(request, cancellationToken);

    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        => eventPublisher.Publish(@event, cancellationToken);

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
        => eventPublisher.Subscribe(action);
}