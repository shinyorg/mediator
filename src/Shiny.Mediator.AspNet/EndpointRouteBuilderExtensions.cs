using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator;


public static class EndpointRouteBuilderExtensions
{
    #region Requests

    extension(IEndpointRouteBuilder builder)
    {
        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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
        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
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
        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
        public RouteHandlerBuilder MapMediatorStreamGet<TRequest, TResult>(string pattern) where TRequest : IStreamRequest<TResult>
            => builder.MapGet(
                pattern,
                async (
                    HttpContext http,
                    [FromServices] IMediator mediator,
                    [FromServices] ISerializerService serializer,
                    [AsParameters] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    await foreach (var item in result)
                    {
                        var json = serializer.Serialize(item);
                        await http.Response.WriteAsync(json, cancellationToken);
                        await http.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            )
            .Produces<TResult>(StatusCodes.Status200OK);


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
        public RouteHandlerBuilder MapMediatorStreamPost<TRequest, TResult>(string pattern) where TRequest : IStreamRequest<TResult>
            => builder.MapGet(
                pattern,
                async (
                    HttpContext http,
                    [FromServices] IMediator mediator,
                    [FromServices] ISerializerService serializer,
                    [AsParameters] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    await foreach (var item in result)
                    {
                        var json = serializer.Serialize(item);
                        await http.Response.WriteAsync(json, cancellationToken);
                        await http.Response.Body.FlushAsync(cancellationToken);
                    }
                }
            )
            .Produces<TResult>(StatusCodes.Status200OK);


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
        public RouteHandlerBuilder MapMediatorServerSentEventsGet<TRequest, TResult>(string pattern, string? eventName = null) where TRequest : IStreamRequest<TResult>
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

                    return TypedResults.ServerSentEvents(result.UnwrapMediatorAsyncEnumerable(), eventName);
                }
            );


        [RequiresDynamicCode("Endpoint routing uses reflection on the supplied delegate and its parameters")]
        public RouteHandlerBuilder MapMediatorServerSentEventsPost<TRequest, TResult>(string pattern, string? eventName = null)
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

                    return TypedResults.ServerSentEvents(result.UnwrapMediatorAsyncEnumerable(), eventName);
                }
            );
    }


    #endregion
}
