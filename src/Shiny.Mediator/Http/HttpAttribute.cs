namespace Shiny.Mediator.Http;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class HttpAttribute(HttpMethod method, string route) : Attribute
{
    public HttpMethod Method => method;
    public string Route => route;
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public class HttpParameterAttribute(HttpParameterType parameterType) : Attribute
{
    public HttpParameterType Type => parameterType;
}

public enum HttpParameterType
{
    Header,
    Query,
    // Cookie,
    Path,
    Body
}