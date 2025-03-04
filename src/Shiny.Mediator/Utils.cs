using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Shiny.Mediator;


public static class Utils
{
    public static IConfigurationSection? GetHandlerSection(this IConfiguration config, string module, object request, object? handler)
    {
        var moduleCfg = config.GetSection("Mediator:" + module);
        if (moduleCfg.Exists())
            return moduleCfg;
        
        var ct = request.GetType();
        if (handler != null)
        {
            var ht = handler.GetType();

            var cfg = moduleCfg.GetHandlerSubSectionOrdered(
                $"{ct.Namespace}.{ct.Name}",
                $"{ht.Namespace}.{ht.Name}",
                $"{ct.Namespace}.*",
                $"{ht.Namespace}.*",
                "*"
            );
            return cfg;
        }

        return null;
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
    

    // TODO: AOT hated
    public static TAttribute? GetHandlerHandleMethodAttribute<TCommand, TAttribute>(this ICommandHandler handler) where TAttribute : Attribute where TCommand : ICommand
        => handler
            .GetType()
            .GetMethod(
                "Handle", 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                CallingConventions.Any,
                [ typeof(TCommand), typeof(IMediatorContext), typeof(CancellationToken) ],
                null
            )!
            .GetCustomAttribute<TAttribute>();
    
    
    // TODO: AOT hated
    public static TAttribute? GetHandlerHandleMethodAttribute<TRequest, TAttribute>(this IRequestHandler handler) where TAttribute : Attribute
        => handler
            .GetType()
            .GetMethod(
                "Handle", 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                CallingConventions.Any,
                [ typeof(TRequest), typeof(IMediatorContext), typeof(CancellationToken) ],
                null
            )!
            .GetCustomAttribute<TAttribute>();
   
   
    // TODO: AOT hated
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
                [ typeof(TEvent), typeof(IMediatorContext), typeof(CancellationToken) ],
                null
            )!
            .GetCustomAttribute<TAttribute>();
}