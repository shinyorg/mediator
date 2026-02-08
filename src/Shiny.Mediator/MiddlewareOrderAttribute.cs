namespace Shiny.Mediator;

/// <summary>
/// Controls the execution order of a middleware class within the pipeline. Lower values execute first
/// (outermost in the pipeline), higher values execute closer to the handler. Middleware without this
/// attribute defaults to order 0. Middleware with the same order preserves DI registration order (stable sort).
/// </summary>
/// <param name="order">The execution order value. Lower values run first.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class MiddlewareOrderAttribute(int order) : Attribute
{
    /// <summary>
    /// Gets the execution order value. Lower values run first (outermost in the pipeline).
    /// </summary>
    public int Order => order;
}
