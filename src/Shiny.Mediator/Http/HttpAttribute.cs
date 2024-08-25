namespace Shiny.Mediator.Http;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class HttpAttribute(HttpMethod method, string route) : Attribute
{
    public HttpMethod Method => method;
    public string Route => route;
}