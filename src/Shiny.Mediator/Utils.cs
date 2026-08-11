using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator;


/// <summary>
/// Miscellaneous helpers used by mediator infrastructure - fire-and-forget task helpers, configuration lookup
/// scoped to a handler/contract pair, and source-generator-friendly attribute retrieval.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Treats <paramref name="task"/> as fire-and-forget, routing any unhandled exception to <paramref name="onError"/>.
    /// </summary>
    public static void RunInBackground(this Task task, Action<Exception> onError)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                onError(x.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);

    /// <summary>
    /// Treats <paramref name="task"/> as fire-and-forget, logging any unhandled exception to <paramref name="errorLogger"/>.
    /// When <paramref name="errorLogger"/> is <c>null</c>, faults are swallowed.
    /// </summary>
    public static void RunInBackground(this Task task, ILogger? errorLogger)
        => task.ContinueWith(x =>
        {
            if (x.Exception != null)
                errorLogger?.LogError(x.Exception, "Fire & Forget trapped error");
        }, TaskContinuationOptions.OnlyOnFaulted);


    /// <summary>
    /// Locates the configuration section under <c>Mediator:{module}</c> that applies to the current context's
    /// message and handler. Returns <c>null</c> when no matching section exists or when
    /// <paramref name="config"/> is <c>null</c>.
    /// </summary>
    public static IConfigurationSection? GetHandlerSection(
        this IMediatorContext context,
        IConfiguration? config,
        string module
    ) => config.GetHandlerSection(module, context.Message, context.MessageHandler);

    /// <summary>
    /// Locates the configuration section under <c>Mediator:{module}</c> matching <paramref name="request"/> and
    /// <paramref name="handler"/>. Looks up by full type name, namespace wildcard, and a final <c>*</c> catch-all in priority order.
    /// Returns <c>null</c> when <paramref name="config"/> is <c>null</c>.
    /// </summary>
    public static IConfigurationSection? GetHandlerSection(
        this IConfiguration? config,
        string module,
        object request,
        object? handler
    )
    {
        if (config == null)
            return null;

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


    /// <summary>
    /// Retrieves a source-generated <see cref="MediatorMiddlewareAttribute"/> of type <typeparamref name="T"/>
    /// from <paramref name="handler"/> for <paramref name="message"/>. Returns <c>null</c> when the handler does
    /// not expose attribute metadata via <see cref="IHandlerAttributeMarker"/>.
    /// </summary>
    public static T? GetHandlerAttribute<T>(object handler, object message) where T : MediatorMiddlewareAttribute
    {
        if (handler is IHandlerAttributeMarker marker)
            return marker.GetAttribute<T>(message);

        return null;
    }


    /// <summary>
    /// Retrieves a source-generated <see cref="MediatorMiddlewareAttribute"/> of type <typeparamref name="T"/>
    /// from the current context's handler for its current message.
    /// </summary>
    public static T? GetHandlerAttribute<T>(this IMediatorContext context) where T : MediatorMiddlewareAttribute =>
        Utils.GetHandlerAttribute<T>(context.MessageHandler!, context.Message!);
}
