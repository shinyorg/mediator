using Shiny.AppFunctions;

namespace Shiny.Mediator;


/// <summary>
/// Reads the <see cref="AppFunctionContext"/> of an app function call from inside mediator handlers and middleware.
/// </summary>
public static class AppFunctionMediatorContextExtensions
{
    /// <summary>
    /// The <see cref="AppFunctionContext"/> when this execution (or one of its parents) was started by Siri,
    /// Shortcuts, an Android agent or <see cref="AppFunctionDispatcher"/>; otherwise <c>null</c>.
    /// </summary>
    public static AppFunctionContext? GetAppFunctionContext(this IMediatorContext context)
    {
        // headers are not copied to child contexts, so walk up to the call that came from the app function
        IMediatorContext? current = context;
        while (current != null)
        {
            var afc = current.TryGetValue<AppFunctionContext>(MediatorAppFunctions.AppFunctionContextHeader);
            if (afc != null)
                return afc;

            current = current.Parent;
        }
        return null;
    }


    /// <summary>
    /// True when this execution was started by an app function call.
    /// </summary>
    public static bool IsAppFunctionCall(this IMediatorContext context)
        => context.GetAppFunctionContext() != null;


    /// <summary>
    /// Sets the text Siri shows or speaks (Android returns it alongside the result). Does nothing when this
    /// execution was not started by an app function call.
    /// </summary>
    public static IMediatorContext SayToAssistant(this IMediatorContext context, string dialog)
    {
        context.GetAppFunctionContext()?.Say(dialog);
        return context;
    }
}
