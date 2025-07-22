// namespace Shiny.Mediator.Http;
//
// public interface IHttpResponseProcessor
// {
//     Task ProcessResponse(
//         HttpResponseMessage response, 
//         IMediatorContext context, 
//         CancellationToken cancellationToken
//     );
// }

// using Shiny.Mediator.Http;
//
// namespace Shiny.Mediator.Infrastructure.Impl;
//
//
// public class DebugHttpResponseProcessor : IHttpResponseProcessor
// {
//     public Task ProcessResponse(HttpResponseMessage response, IMediatorContext context, CancellationToken cancellationToken)
//     {
//         
//
//         return Task.CompletedTask;
//     }
// }


// using Shiny.Mediator.Http;
//
// namespace Shiny.Mediator.Infrastructure.Impl;
//
//
// public class DefaultHttpResponseProcessor : IHttpResponseProcessor
// {
//     public Task ProcessResponse(
//         HttpResponseMessage response, 
//         IMediatorContext context,
//         CancellationToken cancellationToken
//     )
//     {
//         //response.StatusCode == HttpStatusCode.NotExtended // 510
//         // response.StatusCode == HttpStatusCode.UnprocessableEntity // 422 - turn into a ValidateResult/Exception
//         response.EnsureSuccessStatusCode();
//         return Task.CompletedTask;
//     }
// }