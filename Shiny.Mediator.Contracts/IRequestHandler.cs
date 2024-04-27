namespace Shiny.Mediator;

public interface IRequestHandler<TCommand> where TCommand : IRequest
{
    Task Handle(TCommand command, CancellationToken cancellationToken);
}


public interface IRequestHandler<TCommand, TResult> where TCommand : IRequest<TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken);
}