namespace Shiny.Mediator;


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class MediatorMiddlewareAttribute : Attribute;

public interface IHandlerAttributeMarker
{
    T? GetAttribute<T>(object message) where T : MediatorMiddlewareAttribute;
}