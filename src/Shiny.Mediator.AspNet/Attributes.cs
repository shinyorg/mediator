namespace Shiny.Mediator;

// [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// public class MediatorHttpGetAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Get);
//
// [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// public class MediatorHttpDeleteAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Delete);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpPostAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Post);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpPutAttribute(string uriTemplate) : MediatorHttpAttribute(uriTemplate, HttpMethod.Put);


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MediatorHttpAttribute(string uriTemplate, HttpMethod httpMethod) : Attribute
{
    public string UriTemplate => uriTemplate;
    public HttpMethod Method => httpMethod;
}