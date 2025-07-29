// namespace Shiny.Mediator.Sentry;

// public class SentryRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
// {
//     // fingerprint vs span set
//     public async Task<TResult> Process(
//         RequestContext<TRequest> context, 
//         RequestHandlerDelegate<TResult> next, 
//         CancellationToken cancellationToken
//     )
//     {
//         var transaction = SentrySdk.StartTransaction("mediator", "request");
//         var span = transaction.StartChild(context.Handler.GetType().FullName!);
//         
//         var requestKey = ContractUtils.GetRequestKey(context.Request!);
//         span.SetData("RequestKey", requestKey);
//
//         var result = await next().ConfigureAwait(false);
//
//         // tap headers on to span AFTER request - do we care if this changed before vs after? can I denote this in sentry?
//         foreach (var header in context.Values)
//             span.SetData(header.Key, header.Value);
//
//         span.Finish();
//         transaction.Finish();
//         
//         return result;
//     }
// }

// public class SentryCommandMiddleware<TCommand> : ICommandMiddleware<TCommand> where TCommand : ICommand
// {
//     public async Task Process(CommandContext<TCommand> context, CommandHandlerDelegate next, CancellationToken cancellationToken)
//     {
//         var transaction = SentrySdk.StartTransaction("mediator", "event");
//         var span = transaction.StartChild(context.Handler.GetType().FullName!);
//         await next().ConfigureAwait(false);
//         foreach (var header in context.Values)
//             span.SetData(header.Key, header.Value);
//         
//         span.Finish();
//         transaction.Finish();
//     }
// }

// public class SentryEventMiddleware<TEvent> : IEventMiddleware<TEvent> where TEvent : IEvent
// {
//     // would be nice to see a transaction across the event spray
//     public async Task Process(
//         EventContext<TEvent> context, 
//         EventHandlerDelegate next, 
//         CancellationToken cancellationToken
//     )
//     {
//         var transaction = SentrySdk.StartTransaction("mediator", "event");
//         var span = transaction.StartChild(context.Handler.GetType().FullName!);
//         
//         await next().ConfigureAwait(false);
//         foreach (var header in context.Values)
//             span.SetData(header.Key, header.Value);
//         
//         span.Finish();
//         transaction.Finish();
//     }
// }

// public class SentryStreamRequestMiddleware<TRequest, TResult> : IStreamRequestMiddleware<TRequest, TResult> where TRequest : IStreamRequest<TResult>
// {
//     public async IAsyncEnumerable<TResult> Process(
//         RequestContext<TRequest> context, 
//         StreamRequestHandlerDelegate<TResult> next,
//         CancellationToken cancellationToken
//     )
//     {
//         var transaction = SentrySdk.StartTransaction("mediator", "stream");
//         var span = transaction.StartChild(context.Handler.GetType().FullName!);
//         var nxt = next().GetAsyncEnumerator(cancellationToken);
//         
//         var requestKey = ContractUtils.GetRequestKey(context.Request!);
//         span.SetData("RequestKey", requestKey);
//         
//         var moveSpan = span.StartChild("initial_movenext");
//         while (await nxt.MoveNextAsync() && !cancellationToken.IsCancellationRequested)
//         {
//             yield return nxt.Current;
//             moveSpan.Finish();
//             moveSpan = span.StartChild("movenext");
//         }
//         span.Finish();
//         transaction.Finish();
//     }
// }



// public class SentryExceptionHandler : IExceptionHandler
// {
//     public async Task<bool> Handle(object message, object handler, Exception exception)
//     {
//         SentrySdk.CaptureException(exception);
//         return false;
//     }
// }