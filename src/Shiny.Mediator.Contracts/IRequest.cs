namespace Shiny.Mediator;


public interface IBaseRequest<out TResult>
{
}

public interface IRequest : IBaseRequest<Unit>
{
}


public interface IRequest<out TResult> : IBaseRequest<TResult>
{
}

// public interface IStreamRequest<out TResult> : IRequest<IAsyncEnumerable<TResult>> 
// {
// }