namespace Shiny.Mediator;

// TODO: what about T?
public interface IErrorHandler
{
    Task Handle(MediatorErrorContext context);
}

public record MediatorErrorContext(
    object Message,
    bool IsCommand,
    Exception Exception
);