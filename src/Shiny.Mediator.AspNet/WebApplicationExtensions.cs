using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


// TODO: type implements multiple handlers - move attribute to Handle method?
public static class WebApplicationExtensions
{
    static readonly MethodInfo mapCommandType;
    static readonly MethodInfo mapRequestType;
    static readonly MethodInfo mapStreamType;
    
    static WebApplicationExtensions()
    {
        mapRequestType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapResultType), BindingFlags.Static | BindingFlags.Public)!;
        mapStreamType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapStreamType), BindingFlags.Static | BindingFlags.Public)!;
        mapCommandType = typeof(WebAppMap).GetMethod(nameof(WebAppMap.MapCommandType), BindingFlags.Static | BindingFlags.Public)!;
    }


    /// <summary>
    /// This will use reflection to scan all attributes against all handlers registered in your service collection 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static WebApplication MapShinyMediatorEndpointHandlers(this WebApplication app, IServiceCollection services)
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
        if (IsCommandHandler(type))
        {
            var attribute = type.GetCustomAttribute<MediatorHttpAttribute>();
            if (attribute != null)
                MapCommand(app, type, attribute);
        }
        else if (IsRequestHandler(type))
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

    
    static bool IsCommandHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
    
    
    static bool IsRequestHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
    
    
    static bool IsStreamHandler(Type type) => type
        .GetInterfaces()
        .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IStreamRequestHandler<,>));
    
    
    static void MapCommand(WebApplication app, Type handlerType, MediatorHttpAttribute attribute)
    {
        var requestType = handlerType
            .GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
            .Select(x => x.GetGenericArguments().First())
            .First();
        
        mapCommandType
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

        mapRequestType
            .MakeGenericMethod(requestType[0], requestType[1])
            .Invoke(null, [app, attribute]);
    }
}


public class WebAppMap
{
    public static void MapCommandType<TCommand>(WebApplication app, MediatorHttpAttribute attribute) where TCommand : ICommand
    {
        attribute.Tags ??= [$"{typeof(TCommand).Name}s"];
        RouteHandlerBuilder routerBuilder;
        
        if (attribute.Method == HttpMethod.Post)
        {
            routerBuilder = app.MapMediatorPost<TCommand>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapMediatorPut<TCommand>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Get)
        {
            routerBuilder = app.MapMediatorGet<TCommand>(attribute.UriTemplate);
        } 
        else if (attribute.Method == HttpMethod.Delete)
        {
            routerBuilder = app.MapMediatorDelete<TCommand>(attribute.UriTemplate);
        } 
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TCommand).FullName}` - Can only be PUT/POST");
        }
        Visit(routerBuilder, attribute);
    }
    
    
    public static void MapStreamType<TRequest, TResult>(WebApplication app, MediatorHttpAttribute attribute) where TRequest : IStreamRequest<TResult>
    {
        attribute.Tags ??= [$"{typeof(TRequest).Name}s"];
        RouteHandlerBuilder routerBuilder;
        
        if (attribute.Method == HttpMethod.Post)
        {
            routerBuilder = app.MapMediatorStreamPost<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapMediatorStreamPut<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Get)
        {
            routerBuilder = app.MapMediatorStreamGet<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Delete)
        {
            routerBuilder = app.MapMediatorStreamDelete<TRequest, TResult>(attribute.UriTemplate);
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
            routerBuilder = app.MapMediatorPost<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Put)
        {
            routerBuilder = app.MapMediatorPut<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Get)
        {
            routerBuilder = app.MapMediatorGet<TRequest, TResult>(attribute.UriTemplate);
        }
        else if (attribute.Method == HttpMethod.Delete)
        {
            routerBuilder = app.MapMediatorDelete<TRequest, TResult>(attribute.UriTemplate);
        }
        else
        {
            throw new InvalidOperationException($"Invalid Mediator Endpoint on `{typeof(TRequest).FullName}` - Can only be PUT/POST");
        }

        Visit(routerBuilder, attribute);
    }


    static void Visit(RouteHandlerBuilder routeBuilder, MediatorHttpAttribute attribute)
    {
        routeBuilder.WithName(attribute.OperationId);
        
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