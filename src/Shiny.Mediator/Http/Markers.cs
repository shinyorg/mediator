namespace Shiny.Mediator.Http;

public interface IHttpRequest<TResult> : IRequest<TResult>;

public interface IHttpStreamRequest<T> : IStreamRequest<T>
{
    /// <summary>
    /// Null will use auto detection based on Content-Type
    /// </summary>
    HttpStreamType? StreamType { get; set; }
}

public interface IHttpRequestDecorator
{
    Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, CancellationToken cancellationToken);
}

// public interface IHttpResponseProcessor
// {
//     Task Process(HttpResponseMessage httpMessage, IMediatorContext context, CancellationToken cancellationToken);
// }

public enum HttpStreamType
{
    ServerSentEvents = 1,
    PlainStream = 2
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class HttpAttribute(HttpVerb httpVerb, string route) : Attribute
{
    public HttpVerb Verb => httpVerb;
    public string Route => route;
}


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public class HttpParameterAttribute(HttpParameterType parameterType, string? parameterName = null) : Attribute
{
    public HttpParameterType Type => parameterType;
    public string? ParameterName => parameterName;
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