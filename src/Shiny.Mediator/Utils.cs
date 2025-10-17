using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator;


public static class Utils
{
    /// <summary>
    /// Fire & Forget task pattern
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onError"></param>
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    
    /// <summary>
    /// Fire & Forget task pattern that logs errors
    /// </summary>
    /// <param name="task"></param>
    /// <param name="errorLogger"></param>
    public static void RunInBackground(this Task task, ILogger errorLogger)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                errorLogger.LogError(x.Exception, "Fire & Forget trapped error");
        }, TaskContinuationOptions.OnlyOnFaulted);


    public static IConfigurationSection? GetHandlerSection(
        this IMediatorContext context,
        IConfiguration config,
        string module
    ) => config.GetHandlerSection(module, context.Message, context.MessageHandler);
    
    public static IConfigurationSection? GetHandlerSection(
        this IConfiguration config, 
        string module, 
        object request, 
        object? handler
    )
    {
        var moduleCfg = config.GetSection("Mediator:" + module);
        if (!moduleCfg.Exists())
            return null;
        
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


    public static T? GetHandlerAttribute<T>(object handler, object message) where T : MediatorMiddlewareAttribute
    {
        if (handler is IHandlerAttributeMarker marker)
            return marker.GetAttribute<T>(message);

        return null;
    }


    public static T? GetHandlerAttribute<T>(this IMediatorContext context) where T : MediatorMiddlewareAttribute =>
        Utils.GetHandlerAttribute<T>(context.MessageHandler!, context.Message!);
}