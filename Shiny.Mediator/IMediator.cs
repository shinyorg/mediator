namespace Shiny.Mediator;


public interface IMediator
{
    Task Send<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : IRequest;

    Task<TResult> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : IRequest<TResult>;

    // TODO: will need pipelines for logging on fire and forget
    Task Publish<TEvent>(
        TEvent @event, 
        bool fireAndForget = true, 
        bool executeInParallel = true, 
        CancellationToken cancellationToken = default
    ) where TEvent : IEvent;
}