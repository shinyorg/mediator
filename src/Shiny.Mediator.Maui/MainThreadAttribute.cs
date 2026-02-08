namespace Shiny.Mediator;

/// <summary>
/// Ensures the handler method executes on the main/UI thread. This is required in .NET MAUI when
/// the handler needs to update UI elements, display dialogs, navigate, or request device permissions.
/// The handler class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class MainThreadAttribute : MediatorMiddlewareAttribute;