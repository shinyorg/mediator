using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


// TODO: IStreamRequest
// TODO: delete/get
// TODO: type implements multiple handlers - move attribute to Handle method?
public static class WebApplicationExtensions
{
    static readonly MethodInfo mapVoidType;
    static readonly MethodInfo mapResultType;
    static readonly MethodInfo mapStreamType;
    
    static WebApplicationExtensions()
    {
        mapResultType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapResultType), BindingFlags.Static | BindingFlags.Public)!;
        mapStreamType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapStreamType), BindingFlags.Static | BindingFlags.Public)!;
        mapVoidType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapVoidType), BindingFlags.Static | BindingFlags.Public)!;
    }
    
    
    public static WebApplication UseShinyMediatorEndpointHandlers(this WebApplication app, IServiceCollection services)
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
        else if (IsStreamHandler(type))
        {
            var attribute = type.GetCustomAttribute<MediatorHttpAttribute>();
            if (attribute != null)
                MapStream(app, type, attribute);
        }
    }

    
    static bool IsVoidHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<>));
    
    
    static bool IsResultHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    
    
    static bool IsStreamHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>));
    
    
    static void MapVoid(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType
            .GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
            .Select(x => x.GetGenericArguments().First())
            .First();
        
        mapVoidType
            .MakeGenericMethod(requestType)
            .Invoke(null, [app, attribute]);
    }


    static void MapStream(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType
            .GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>))
            .Select(x => x.GetGenericArguments())
            .First();

        mapStreamType
            .MakeGenericMethod(requestType[0], requestType[1])
            .Invoke(null, [app, attribute]);
    }
    
    
    static void MapResult(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType
            .GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Select(x => x.GetGenericArguments())
            .First();

        mapResultType
            .MakeGenericMethod(requestType[0], requestType[1])
            .Invoke(null, [app, attribute]);
    }
}


public class WebAppMap
{
    public static void MapVoidType<TRequest>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IRequest
    {
        attribute.Tags ??= [$"{typeof(TRequest).Name}s"];
        RouteHandlerBuilder routerBuilder;
        
        if (attribute.Method == HttpMethod.Post)
        {
            routerBuilder = app.MapPost(
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
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapPut(
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
        }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }
        Visit(routerBuilder, attribute);
    }
    
    
    public static void MapStreamType<TRequest, TResult>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IStreamRequest<TResult>
    {
        attribute.Tags ??= [$"{typeof(TRequest).Name}s"];
        RouteHandlerBuilder routerBuilder;
        
        if (attribute.Method == HttpMethod.Post)
        {
            routerBuilder = app.MapPost(
                attribute.UriTemplate,
                (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) => mediator.Request(request, cancellationToken)
            );
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapPut(
                attribute.UriTemplate,
                (
                    [FromServices] IMediator mediator,
                    [FromBody] TRequest request,
                    CancellationToken cancellationToken
                ) => mediator.Request(request, cancellationToken)
            );
        }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }

        Visit(routerBuilder, attribute);
    }
    

    public static void MapResultType<TRequest, TResult>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IRequest<TResult>
    {
        attribute.Tags ??= [$"{typeof(TRequest).Name}s"];
        RouteHandlerBuilder routerBuilder;
        
        if (attribute.Method == HttpMethod.Post)
        {
            routerBuilder = app.MapPost(
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
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapPut(
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
        }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }

        Visit(routerBuilder, attribute);
    }


    static void Visit(RouteHandlerBuilder routeBuilder, MediatorHttpAttribute attribute)
    {
        if (attribute.Name != null)
            routeBuilder.WithName(attribute.Name);
        
        if (attribute.Summary != null)
             routeBuilder.WithSummary(attribute.Summary);

        if (attribute.Tags != null)
            routeBuilder.WithTags(attribute.Tags);
        
        if (attribute.Description != null)
            routeBuilder.WithDescription(attribute.Description);
        
        if (attribute.CachePolicy != null)
            routeBuilder.CacheOutput(attribute.CachePolicy);
        
        if (attribute.AllowAnonymous)
            routeBuilder.AllowAnonymous();

        if (attribute.GroupName != null)
            routeBuilder.WithGroupName(attribute.GroupName);
        
        if (attribute.DisplayName != null)
            routeBuilder.WithDisplayName(attribute.DisplayName);

        if (attribute.ExcludeFromDescription)
            routeBuilder.ExcludeFromDescription();
        
        if (attribute.CorsPolicy != null)
            routeBuilder.RequireCors(attribute.CorsPolicy);

        if (attribute.RateLimitingPolicy != null)
            routeBuilder.RequireRateLimiting(attribute.RateLimitingPolicy);
        
        if (attribute.UseOpenApi)
            routeBuilder.WithOpenApi();

        if (attribute.AuthorizationPolicies != null || attribute.RequiresAuthorization)
        {
            if (attribute.AuthorizationPolicies == null)
                routeBuilder.RequireAuthorization();
            else
                routeBuilder.RequireAuthorization(attribute.AuthorizationPolicies);
        }
        // routerBuilder.ProducesProblem()
        // routerBuilder.Produces<>()
        // routerBuilder.Accepts<>()
    }
}