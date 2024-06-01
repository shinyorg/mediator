using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class Mediator(
    IRequestSender requestSender,
    IEventPublisher eventPublisher
) : IMediator
{
    public Task<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
        => requestSender.Send(request, cancellationToken);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        => requestSender.Send(request, cancellationToken);

    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        => eventPublisher.Publish(@event, cancellationToken);

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
        => eventPublisher.Subscribe(action);
}