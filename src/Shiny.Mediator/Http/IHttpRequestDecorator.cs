namespace Shiny.Mediator.Http;

public interface IHttpRequestDecorator<TRequest, TResult> where TRequest : IHttpRequest<TResult>
{
    Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, TRequest request);
}