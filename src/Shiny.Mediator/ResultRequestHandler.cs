// namespace Shiny.Mediator;
//
// public abstract class ResultRequestHandler<TRequest, TResult> : IRequestHandler<TRequest, TResult>
//     where TRequest : IRequest<TResult>
// {
//     public Task<TResult> Handle(TRequest request, IMediatorContext context, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
// }
//
// public sealed class Result<TValue>
// {
//     public TValue? Value { get; private set; }
//     public Exception? Exception { get; private set; }
// } 