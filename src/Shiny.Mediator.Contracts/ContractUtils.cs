using System.Reflection;

namespace Shiny.Mediator;


public static class ContractUtils
{
    /// <summary>
    /// Builds a key that can be used for caching/offline/storage unique identification
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="tryKeyReflect">If your object is not IRequestKey, this will use reflection to build a key from public get/set values in your object otherwise it will fallback to namespace_typename</param>
    /// <returns></returns>
    public static string GetRequestKey(object obj, bool tryKeyReflect = false)
    {
        if (obj is IRequestKey key)
            return key.GetKey();

        if (tryKeyReflect)
            return GetKeyFromReflection(obj);
        
        var t = obj.GetType();
        var stringKey = $"{t.Namespace}_{t.Name}";
        return stringKey;
    }


    /// <summary>
    /// Provides an easy and fast way to get a key for a request
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string ReflectKey(this IRequestKey request)
        => GetKeyFromReflection(request);


    static string GetKeyFromReflection(object request)
    {
        var t = request.GetType();
        var key = request.GetType().FullName!;
        var props = t
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .OrderBy(x => x.Name)
            .ToList();

        foreach (var prop in props)
        {
            var value = prop.GetValue(request);
            if (value != null)
                key += $"_{prop.Name}_{value}";
        }

        return key;
    }
}