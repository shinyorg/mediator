using System.Reflection;

namespace Shiny.Mediator.Infrastructure;

public static class Utils
{
    public static string GetRequestKey(object request)
    {
        var t = request.GetType();
        var prop = t.GetProperty("CacheKey", BindingFlags.Instance | BindingFlags.Public);
        string key = null!;
        
        if (prop != null && prop.PropertyType == typeof(string))
        {
            var pv = (string)prop.GetValue(request);
            key = $"{t.Name}_{pv}";
        }
        else
        {
            key = $"{t.Namespace}_{t.Name}";
        }
        return key;
    }
}