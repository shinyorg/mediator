using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


// TODO: IStreamRequest
// TODO: file uploads
// TODO: delete/get
public static class WebApplicationExtensions
{
    public static WebApplication UseMappedShinyMediatorHandlers(this WebApplication app, IServiceCollection services)
    {
        foreach (var service in services)
        {
            if (service.ImplementationType != null)
            {
                TryMap(app, service.ImplementationType);
            } 
            else if (service.ImplementationInstance != null)
            {
                TryMap(app, service.ImplementationInstance.GetType());
            }
        }
        return app;
    }


    static void TryMap(WebApplication app, Type type)
    {
        if (IsVoidHandler(type))
        {
            var attribute = type.GetCustomAttribute<MediatorHttpAttribute>();
            if (attribute != null)
                MapVoid(app, type, attribute);
        }
        else if (IsResultHandler(type))
        {
            var attribute = type.GetCustomAttribute<MediatorHttpAttribute>();
            if (attribute != null)
                MapResult(app, type, attribute);
        } 
    }

    
    static bool IsVoidHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<>));
    
    
    static bool IsResultHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    
    
    static void MapVoid(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType.GetGenericArguments().First();
        var mapMethod = typeof(WebApplicationExtensions).GetMethod(nameof(MapVoidType))!;
        var method = mapMethod.MakeGenericMethod(requestType);
        method.Invoke(null, [app, attribute]);
    }

    
    static void MapResult(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType.GetGenericArguments().First();
        var mapMethod = typeof(WebApplicationExtensions).GetMethod(nameof(MapResultType))!;
        var method = mapMethod.MakeGenericMethod(requestType);
        method.Invoke(null, [app, attribute]);
    }
    

    
    // .WithName("GetWeatherForecast")
    //     .WithOpenApi();
    static void MapVoidType<TRequest>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IRequest
    {
        if (attribute.Method == HttpMethod.Post)
        {
            app.MapPost(
                attribute.UriTemplate,
                async (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    await mediator
                        .Send(request, cancellationToken)
                        .ConfigureAwait(false);

                    return Results.Ok();
                }
            );
            //     .WithTags($"{type.Name}s");
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            app.MapPut(
                attribute.UriTemplate,
                async (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    await mediator
                        .Send(request, cancellationToken)
                        .ConfigureAwait(false);
                    
                    return Results.Ok();
                }
            );
            //     .WithTags($"{type.Name}s");
        }
        // else if (attribute.Method == HttpMethod.Delete)
        // {
        //     // app.MapDelete()
        // }
        // else if (attribute.Method == HttpMethod.Get)
        // {
        //     
        // }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }
    }
    

    static void MapResultType<TRequest, TResult>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IRequest<TResult>
    {
        if (attribute.Method == HttpMethod.Post)
        {
            app.MapPost(
                attribute.UriTemplate,
                async (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                }
            );
            //     .WithTags($"{type.Name}s");
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            app.MapPut(
                attribute.UriTemplate,
                async (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await mediator
                        .Request(request, cancellationToken)
                        .ConfigureAwait(false);

                    return Results.Ok(result);
                }
            );
            //     .WithTags($"{type.Name}s");
        }
        // else if (attribute.Method == HttpMethod.Delete)
        // {
        //     // app.MapDelete()
        // }
        // else if (attribute.Method == HttpMethod.Get)
        // {
        //     
        // }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }
    }
}