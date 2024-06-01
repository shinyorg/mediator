// using Shiny.Mediator;
//
// // TODO: how do I register an "ALL" middleware
//
// // TODO: execution duration timer
//
// // TODO: catch all could be IRequest or IRequest<T>?  Could use an IRequest<Void>?
// public interface IRequestMiddleware<TRequest, TResult> where TRequest : IRequest<TResult>
// {
//     // intercept with connectivity, if offline go to cache, if online go to remote
//     // if went to remote, post execute stores to cache
//     Task<TResult> Process(TRequest request, IRequestMiddleware<TRequest, TResult> next, CancellationToken cancellationToken);
// }