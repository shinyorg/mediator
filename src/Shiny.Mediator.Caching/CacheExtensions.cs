using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Mediator.Caching.Infrastructure;
using Shiny.Mediator.Middleware;

namespace Shiny.Mediator;


public static class CacheExtensions
{
    static readonly FieldInfo CoherentStateField = typeof(MemoryCache).GetField("_coherentState", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public static ShinyConfigurator AddMemoryCaching(this ShinyConfigurator cfg, Action<MemoryCacheOptions>? configureCache = null)
    {
        cfg.Services.AddMemoryCache(x => configureCache?.Invoke(x));
        cfg.Services.AddSingleton<IEventHandler<FlushAllStoresEvent>, FlushCacheEventHandler>();
        cfg.AddOpenRequestMiddleware(typeof(CachingRequestMiddleware<,>));
        return cfg;
    }


    public static DateTimeOffset? CacheTimestamp(this IRequestContext context)
        => context.TryGetValue<DateTimeOffset>("Cache.Timestamp");
    
    internal static void SetCacheTimestamp(this IRequestContext context, DateTimeOffset timestamp)
        => context.Add("Cache.Timestamp", timestamp);
    
    public static string GetCacheKey(object request)
    {
        if (request is IRequestKey keyProvider)
            return keyProvider.GetKey();
        
        var t = request.GetType();
        var key = $"{t.Namespace}_{t.Name}";
        return key;
    }
    

    public static void Clear(this IMemoryCache cache) 
        => (cache as MemoryCache)?.Clear();


    public static void RemoveByKeyStartsWith(this IMemoryCache cache, string key)
    {
        GetEntries(cache)
            .Select(x => x.Key as string)
            .Where(x => x != null)
            .ToList()
            .ForEach(x => cache.Remove(x!));
    }
    
    
    public static IDictionary<object, ICacheEntry> GetEntries(this IMemoryCache cache)
    {
        var entries = new Dictionary<object, ICacheEntry>();
        var state = CoherentStateField.GetValue(cache);
        if (state == null)
            return entries;
        
        var entryField = state.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
        if (entryField == null)
            return entries;
        
        var dict = entryField.GetValue(state) as IDictionary;
        if (dict == null)
            return entries;

        foreach (DictionaryEntry entry in dict)
            entries.Add(entry.Key, (ICacheEntry)entry.Value!);
        
        return entries;
    }
}