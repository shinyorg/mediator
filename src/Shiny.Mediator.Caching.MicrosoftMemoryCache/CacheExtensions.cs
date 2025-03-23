using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator;


public static class MemoryCacheExtensions
{
    static readonly FieldInfo CoherentStateField = typeof(MemoryCache).GetField("_coherentState", BindingFlags.Instance | BindingFlags.NonPublic)!;
    

    public static ShinyMediatorBuilder AddMemoryCaching(this ShinyMediatorBuilder cfg, Action<MemoryCacheOptions>? configureCache = null)
    {
        cfg.Services.AddMemoryCache(x => configureCache?.Invoke(x));
        cfg.AddCaching<MemoryCacheService>();
        return cfg;
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
#if NET8_0
        TryReflectOut(entries, state, "_entries");
#else 
        TryReflectOut(entries, state, "_stringEntries");
        TryReflectOut(entries, state, "_nonStringEntries");
#endif
        
        return entries;
    }


    static void TryReflectOut(IDictionary<object, ICacheEntry> entries, object state, string fieldName)
    {
        var entryField = state.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (entryField == null)
            return;
        
        var dict = entryField.GetValue(state) as IDictionary;
        if (dict == null)
            return;

        foreach (DictionaryEntry entry in dict)
            entries.Add(entry.Key, (ICacheEntry)entry.Value!);
    }
}