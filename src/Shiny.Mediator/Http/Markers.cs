namespace Shiny.Mediator.Http;


public interface IHttpRequestDecorator
{
    Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, CancellationToken cancellationToken);
}


public interface IServerSentEventsStream;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class GetAttribute(string route) : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class DeleteAttribute(string route) : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PatchAttribute(string route) : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PostAttribute(string route) : Attribute;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class PutAttribute(string route) : Attribute;



[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class HeaderAttribute(string? Name = null) : Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class BodyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class QueryAttribute(string? Name = null) : Attribute;