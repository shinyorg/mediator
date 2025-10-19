using System.Runtime.CompilerServices;

namespace Shiny.Mediator.Infrastructure;


public static class MediatorRegistry
{
    // TODO: allow user to filter stuff out?
    static readonly List<Action<ShinyMediatorBuilder>> callbacks = new();
    
    public static void RegisterCallback(Action<ShinyMediatorBuilder> builderAction, [CallerFilePath] string callerFilePath = "")
    {
        callbacks.Add(builderAction);
    }


    public static void RunCallbacks(ShinyMediatorBuilder builder)
    {
        callbacks.ForEach(callback => callback(builder));
    }
}