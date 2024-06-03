namespace Shiny.Mediator;

public interface IRequest : IRequest<Unit>
{
}


public interface IRequest<out TResponse>
{
}