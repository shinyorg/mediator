using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shiny.Mediator.Infrastructure;

namespace Shiny.Mediator.Impl;


public class FeatureService(IConfiguration config) : IFeatureService
{
    public TConfig? GetIfAvailable<TConfig>(object request, object handler) where TConfig : Attribute
    {
        var attribute = handler
            .GetType()
            .GetMethod(
                "Handle",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                CallingConventions.Any,
                [request.GetType(), typeof(CancellationToken)],
                null
            )!
            .GetCustomAttribute<TConfig>();

        attribute ??= request.GetType().GetCustomAttribute<TConfig>();

        if (attribute == null)
        {

        }
        
        throw new NotImplementedException();
    }

    public TConfig? GetFromConfigIfAvailable<TConfig>(object request, object handler)
    {
        var t = request.GetType();
        var key = $"{t.Namespace}.{t.Name}";
        if (config[key] != null)
        {
                
        }
        // TODO: backup a namespace
        else if (config[$"{t.Namespace}.*"] != null)
        {
                
        }
        else if (config["*"] != null)
        {
                
        }

        return default;
    }
}
/*
public static TAttribute? GetHandlerHandleMethodAttribute<TRequest, TAttribute>(this IRequestHandler handler) where TAttribute : Attribute
   => handler
       .GetType()
       .GetMethod(
           "Handle", 
           BindingFlags.Public | BindingFlags.Instance, 
           null,
           CallingConventions.Any,
           [ typeof(TRequest), typeof(CancellationToken) ],
           null
       )!
       .GetCustomAttribute<TAttribute>();
   
   
public static TAttribute? GetHandlerHandleMethodAttribute<TEvent, TAttribute>(this IEventHandler<TEvent> handler) 
   where TEvent : IEvent
   where TAttribute : Attribute
   => handler
       .GetType()
       .GetMethod(
           "Handle", 
           BindingFlags.Public | BindingFlags.Instance, 
           null,
           CallingConventions.Any,
           [ typeof(TEvent), typeof(CancellationToken) ],
           null
       )!
       .GetCustomAttribute<TAttribute>();
 */