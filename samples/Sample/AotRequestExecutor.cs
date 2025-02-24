// using Sample.Contracts;
// using Shiny.Mediator.Infrastructure;
// using Shiny.Mediator.Infrastructure.Impl;
//
// namespace Sample;
//
//
// public class AotRequestExecutor(IServiceProvider services) : IRequestExecutor
// {
//     public async Task<RequestResult<TResult>> RequestWithContext<TResult>(
//         IRequest<TResult> request, 
//         CancellationToken cancellationToken = default,
//         params IEnumerable<(string Key, object Value)> headers)
//     {
//         using var scope = services.CreateScope();
//         
//         if (request.GetType() == typeof(MyMessageRequest))
//         {
//             var v1 = new RequestResultWrapper<MyMessageRequest, MyMessageResponse>(scope.ServiceProvider, (MyMessageRequest)request, headers, cancellationToken);
//             var result = await v1.Handle().ConfigureAwait(false);
//             return (result as RequestResult<TResult>)!;
//         }
//         
//         throw new InvalidOperationException("Invalid request type");
//     }
//
//     public RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
//         IStreamRequest<TResult> request, 
//         CancellationToken cancellationToken = default,
//         params IEnumerable<(string Key, object Value)> headers)
//     {
//         throw new NotImplementedException();
//     }
// }