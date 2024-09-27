// namespace Shiny.Mediator;
//
//
// public class EventContext
// {
//     // TODO: event types that were fired
//     // TODO: what about middleware that fired per event?
//     public Guid ExecutionId { get; } = Guid.NewGuid();
//     public IReadOnlyDictionary<string, object> Store => this.store;
//     
//     readonly Dictionary<string, object> store = new();
//     public void Add(string key, object value)
//         => this.store.Add(key, value);
// }
//
//
//
// // public static class Test
// // {
// //     // public static PipelineContext RequestAsContext(TRequest request)
// // }
// // TODO: what about events triggered?
// // these are all inserted into a Dictionary from each piece of middleware
// // this class is passed through each piece of middleware
// // public Exception? UserNotificationException { get; set; }
// // public bool IsFromOffline { get; set; }
// // public bool IsFromCache { get; set; }
// // public DateTimeOffset? Timestamp { get; set; }
//
//
// public class RequestContext
// {
//     public Guid ExecutionId { get; } = Guid.NewGuid();
//     public IReadOnlyDictionary<string, object> Store => this.store;
//     
//     // TODO: middleware order and types entered
//     readonly Dictionary<string, object> store = new();
//     public void Add(string key, object value)
//         => this.store.Add(key, value);
//     
//     // public TResult? Result { get; set; }
// }