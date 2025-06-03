namespace Shiny.Mediator.Http;

public interface IHttpRequestDecorator
{
    Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context);
}