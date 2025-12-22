// namespace Shiny.Mediator.Middleware;
//
// [AttributeUsage(AttributeTargets.Method)]
// public class ErrorTrapAttribute : MediatorMiddlewareAttribute;
//
// public static class ErrorTrapMiddlewareExtensions
// {
//     public static ShinyMediatorBuilder UseErrorTrapMiddleware(this ShinyMediatorBuilder builder)
//     {
//         builder.AddOpenCommandMiddleware(typeof(ErrorTrapCommandMiddleware<>));
//         builder.AddOpenEventMiddleware(typeof(ErrorTrapEventMiddleware<>));
//         return builder;
//     }
// }
//
// public class ErrorTrapCommandMiddleware<T> : ICommandMiddleware<T> where T : ICommand
// {
//     public async Task Process(IMediatorContext context, CommandHandlerDelegate next, CancellationToken cancellationToken)
//     {
//         var attr = context.GetHandlerAttribute<ErrorTrapAttribute>();
//         if (attr == null)
//         {
//             await next().ConfigureAwait(false);
//         }
//         else
//         {
//             try
//             {
//                 await next().ConfigureAwait(false);
//             }
//             catch (Exception ex)
//             {
//                 context.Exception = ex;
//             }
//         }
//     }
// }
//
// public class ErrorTrapEventMiddleware<T> : IEventMiddleware<T> where T : IEvent
// {
//     public async Task Process(IMediatorContext context, EventHandlerDelegate next, CancellationToken cancellationToken)
//     {
//         var attr = context.GetHandlerAttribute<ErrorTrapAttribute>();
//         if (attr == null)
//         {
//             await next().ConfigureAwait(false);
//         }
//         else
//         {
//             try
//             {
//                 await next().ConfigureAwait(false);
//             }
//             catch (Exception ex)
//             {
//                 context.Exception = ex;
//             }
//         }
//     }
// }