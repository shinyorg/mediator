using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator.Infrastructure;


public static class Utils
{
    public static IConfigurationSection GetHandlerSection(this IConfiguration config, object request, object handler)
    {
        var t = request.GetType();
        var key = $"{t.Namespace}.{t.Name}";
        if (config[key] != null)
        {
            return config.GetSection(key);
        }
        
        // var t = request.GetType();
        // var key = $"{t.Namespace}.{t.Name}";
        // if (config[key] != null)
        // {
        //         
        // }
        // // TODO: backup a namespace
        // else if (config[$"{t.Namespace}.*"] != null)
        // {
        //         
        // }
        // else if (config["*"] != null)
        // {
        //         
        // }

        return null;
    }
    

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
}