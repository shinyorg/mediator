namespace Shiny.Mediator;

/// <summary>
/// Ensures the handler method executes on the main/UI thread. Use it when the handler needs to update
/// UI elements, display dialogs, navigate, or request device permissions. Supported by the MAUI,
/// Uno Platform and Avalonia integrations. The handler class must be declared as <c>partial</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class MainThreadAttribute : MediatorMiddlewareAttribute;
