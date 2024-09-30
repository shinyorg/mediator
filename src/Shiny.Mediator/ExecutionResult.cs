namespace Shiny.Mediator;

public record ExecutionResult<TResult>(
    ExecutionContext Context,
    TResult Result
);