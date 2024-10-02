using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator;


public static class Utils
{
    public static IConfigurationSection? GetHandlerSection(this IConfiguration config, string module, object request, object handler)
    {
        var moduleCfg = config.GetSection("Mediator:" + module);
        if (!moduleCfg.Exists())
            return null;
        
        var ct = request.GetType();
        var ht = handler.GetType();
        
        var cfg = moduleCfg.GetHandlerSubSectionOrdered(
            $"{ct.Namespace}.{ct.Name}",
            $"{ht.Namespace}.{ht.Name}",
            $"{ct.Namespace}.*",
            $"{ht.Namespace}.*",
            "*"
        );

        return cfg;
        // TODO: sub namespaces
        // ORDER: config, attribute contract, attribute handler
        // config contract order: full type, exact namespace, sub namespace, *
        // config handler order: full type, exact namespace, sub namespace, *
    }


    static IConfigurationSection? GetHandlerSubSectionOrdered(this IConfigurationSection section, params string[] keys)
    {
        IConfigurationSection? result = null;
        var index = 0;

        while (result == null && index != keys.Length)
        {
            var nextKey = keys[index++];
            var tmp = section.GetSection(nextKey);
            if (tmp.Exists())
                result = tmp;
        }
        return result;
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


    public static string GetRequestKey(object request)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";

        return key;
    }
}