using System.Collections.Concurrent;
using System.Reflection;

namespace Shiny.Mediator.Infrastructure;

static class MiddlewareOrderResolver
{
    static readonly ConcurrentDictionary<Type, int> OrderCache = new();

    public static IEnumerable<T> OrderMiddleware<T>(IEnumerable<T> middlewares)
        => middlewares.OrderBy(m => GetOrder(m!));

    static int GetOrder(object middleware)
        => OrderCache.GetOrAdd(
            middleware.GetType(),
            static type => type.GetCustomAttribute<MiddlewareOrderAttribute>()?.Order ?? 0
        );
}
