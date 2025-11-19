using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Shiny.Mediator;


public static class EndpointRouteBuilderExtensions
{
    #region Requests
    
    extension(IEndpointRouteBuilder builder)
    {
        public RouteHandlerBuilder MapMediatorGet<TRequest, TResult>(string pattern) where TRequest : IRequest<TResult>
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


        public RouteHandlerBuilder MapMediatorPost<TRequest, TResult>(string pattern) where TRequest : IRequest<TResult>
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


        public RouteHandlerBuilder MapMediatorPut<TRequest, TResult>(string pattern) where TRequest : IRequest<TResult>
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


        public RouteHandlerBuilder MapMediatorDelete<TRequest, TResult>(string pattern) where TRequest : IRequest<TResult>
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
    }

    #endregion
    
    #region Commands

    extension(IEndpointRouteBuilder builder)
    {
        public RouteHandlerBuilder MapMediatorGet<TCommand>(string pattern) where TCommand : ICommand
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


        public RouteHandlerBuilder MapMediatorDelete<TCommand>(string pattern) where TCommand : ICommand
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


        public RouteHandlerBuilder MapMediatorPut<TCommand>(string pattern) where TCommand : ICommand
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


        public RouteHandlerBuilder MapMediatorPost<TCommand>(string pattern) where TCommand : ICommand
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
    }

    #endregion
    
    #region Stream Requests
    
    extension(IEndpointRouteBuilder builder)
    {
        public RouteHandlerBuilder MapMediatorStreamGet<TRequest, TResult>(string pattern) where TRequest : IStreamRequest<TResult>
            => builder.MapGet(
                pattern, 
                (
                    [FromServices] IMediator mediator,
                    [AsParameters] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    return TypedResults.Ok(UnwrapMediatorAsyncEnumerable(result));
                }
            );


        public RouteHandlerBuilder MapMediatorStreamPost<TRequest, TResult>(string pattern)
            where TRequest : IStreamRequest<TResult>
            => builder.MapPost(
                pattern, 
                (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    return TypedResults.Ok(UnwrapMediatorAsyncEnumerable(result));
                }
            );
    }


    static async IAsyncEnumerable<T> UnwrapMediatorAsyncEnumerable<T>(ConfiguredCancelableAsyncEnumerable<(IMediatorContext Context, T Result)> source)
    {
        await foreach (var item in source)
            yield return item.Result;
    }
    
    #endregion
}