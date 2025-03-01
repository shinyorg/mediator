namespace Shiny.Mediator;

public record RequestResult<TResult>(
    MediatorContext Context,
    TResult Result
);