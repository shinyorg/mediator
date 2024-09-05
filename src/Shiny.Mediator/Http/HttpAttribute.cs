namespace Shiny.Mediator.Http;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class HttpAttribute(HttpVerb httpVerb, string route) : Attribute
{
    public HttpVerb Verb => httpVerb;
    public string Route => route;
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public class HttpParameterAttribute(HttpParameterType parameterType) : Attribute
{
    public HttpParameterType Type => parameterType;
}

public enum HttpVerb
{
    Get,
    Post,
    Put,
    Delete,
    Patch
}

public enum HttpParameterType
{
    Header,
    Query,
    // Cookie,
    Path,
    Body
}