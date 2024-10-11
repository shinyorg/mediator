namespace Shiny.Mediator.Infrastructure.Impl;


public class Mediator(
    IRequestSender requestSender,
    IEventPublisher eventPublisher
) : IMediator
{
    public async Task<TResult> Request<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var result = await requestSender
            .Request(request, cancellationToken)
            .ConfigureAwait(false);
        
        // TODO: tack on event context?
        if (result is IEvent @event)
            await this.Publish(@event, cancellationToken).ConfigureAwait(false);
        
        return result;
    }

    public async Task<ExecutionResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request,
        CancellationToken cancellationToken = default
    )
    {
        var context = await requestSender
            .RequestWithContext(request, cancellationToken)
            .ConfigureAwait(false);
        
        // TODO: tack on event context?
        if (context.Result is IEvent @event)
            await this.Publish(@event, cancellationToken).ConfigureAwait(false);
        
        return context;
    }

    public Task<ExecutionContext> Send(IRequest request, CancellationToken cancellationToken = default)
        => requestSender.Send(request, cancellationToken);

    public IAsyncEnumerable<TResult> Request<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        var context = this.RequestWithContext(request, cancellationToken);
        return context.Result;
    }

    public ExecutionResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(IStreamRequest<TResult> request, CancellationToken cancellationToken = default)
        => requestSender.RequestWithContext(request, cancellationToken);

    public Task<EventAggregatedExecutionContext<TEvent>> Publish<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default,
        bool executeInParallel = true
    ) where TEvent : IEvent => eventPublisher.Publish(@event, cancellationToken, executeInParallel);

    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> action) where TEvent : IEvent
        => eventPublisher.Subscribe(action);
}