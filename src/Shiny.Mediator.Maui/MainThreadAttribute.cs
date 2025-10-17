namespace Shiny.Mediator;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class MainThreadAttribute : MediatorMiddlewareAttribute;