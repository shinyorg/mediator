using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Shiny.Mediator;


public static class EndpointRouteBuilderExtensions
{
    // TODO: streaming to signalr
    
    #region Requests
    
    public static RouteHandlerBuilder MapMediatorGet<TRequest, TResult>(this IEndpointRouteBuilder builder, string pattern)
        where TRequest : IRequest<TResult>
    => builder.MapGet(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [AsParameters] TRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await mediator
                .Request(request, cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(result.Result);
        }
    );
    
    
    public static RouteHandlerBuilder MapMediatorPost<TRequest, TResult>(this IEndpointRouteBuilder builder, string pattern)
        where TRequest : IRequest<TResult>
    => builder.MapPost(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [FromBody] TRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await mediator
                .Request(request, cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(result.Result);
        }
    );
    
    
    public static RouteHandlerBuilder MapMediatorPut<TRequest, TResult>(this IEndpointRouteBuilder builder, string pattern)
        where TRequest : IRequest<TResult>
    => builder.MapPut(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [FromBody] TRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await mediator
                .Request(request, cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(result.Result);
        }
    ); 
    
    
    public static RouteHandlerBuilder MapMediatorDelete<TRequest, TResult>(this IEndpointRouteBuilder builder, string pattern)
        where TRequest : IRequest<TResult>
    => builder.MapDelete(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [AsParameters] TRequest request,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await mediator
                .Request(request, cancellationToken)
                .ConfigureAwait(false);

            return TypedResults.Ok(result.Result);
        }
    );
    
    #endregion
    
    #region Commands

    public static RouteHandlerBuilder MapMediatorGet<TCommand>(this IEndpointRouteBuilder builder, string pattern)
        where TCommand : ICommand
    => builder.MapGet(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [AsParameters] TCommand command,
            CancellationToken cancellationToken
        ) =>
        {
            await mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok();
        }
    );
    
    
    public static RouteHandlerBuilder MapMediatorDelete<TCommand>(this IEndpointRouteBuilder builder, string pattern)
        where TCommand : ICommand
    => builder.MapDelete(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [AsParameters] TCommand command,
            CancellationToken cancellationToken
        ) =>
        {
            await mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok();
        }
    );
    
    
    public static RouteHandlerBuilder MapMediatorPut<TCommand>(this IEndpointRouteBuilder builder, string pattern)
        where TCommand : ICommand
    => builder.MapPut(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [FromBody] TCommand command,
            CancellationToken cancellationToken
        ) =>
        {
            await mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok();
        }
    );
    
    
    public static RouteHandlerBuilder MapMediatorPost<TCommand>(this IEndpointRouteBuilder builder, string pattern)
        where TCommand : ICommand
    => builder.MapPost(
        pattern, 
        async (
            [FromServices] IMediator mediator,
            [FromBody] TCommand command,
            CancellationToken cancellationToken
        ) =>
        {
            await mediator
                .Send(command, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok();
        }
    );
    
    #endregion
}