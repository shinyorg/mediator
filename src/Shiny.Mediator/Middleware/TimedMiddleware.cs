// using System.Diagnostics;
//
// namespace Shiny.Mediator.Middleware;
//
// public class TimedMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
// {
//     public async Task<TResult> Process(TRequest request, IRequestMiddleware<TRequest, TResult> next, CancellationToken cancellationToken)
//     {
//         var sw = new Stopwatch();
//         sw.Start();
//         var result = await next(request, cancellationToken);
//         sw.Stop();
//         
//         // TODO: alert on long?
//         return result;
//     }
// }

