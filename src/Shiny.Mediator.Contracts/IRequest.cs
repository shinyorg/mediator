namespace Shiny.Mediator;

public interface IRequest : IRequest<Unit>
{
}


public interface IRequest<out TResult>
{
}

public interface IStreamRequest<out TResult> : IRequest<IAsyncEnumerable<TResult>> 
{
}