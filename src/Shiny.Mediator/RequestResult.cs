namespace Shiny.Mediator;

public record RequestResult<TResult>(
    RequestContext Context,
    TResult Result
);