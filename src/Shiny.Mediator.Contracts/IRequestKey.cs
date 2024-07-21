using System.Reflection;

namespace Shiny.Mediator;

/// <summary>
/// This is viewed by replay, cache, and various other services where you can control an entry
/// Simply mark your IRequest or IStreamRequest and provide the necessary key to determine uniqueness
/// </summary>
public interface IRequestKey
{
    string GetKey();
}


public static class RequestKeyExtensions
{
    /// <summary>
    /// Provides an easy and fast way to get a key for a request
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string ReflectKey(this IRequestKey request)
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