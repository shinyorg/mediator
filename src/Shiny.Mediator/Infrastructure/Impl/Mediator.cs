using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;


public class Mediator(
    IRequestExecutor requestExecutor, 
    ICommandExecutor commandExecutor, 
    IEventExecutor eventExecutor
) : IMediator
{
    public async Task<RequestResult<TResult>> RequestWithContext<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    )
    {
        var execution = await requestExecutor
            .RequestWithContext(
                request, 
                cancellationToken, 
                headers
            )
            .ConfigureAwait(false);
        
        if (execution.Result is IEvent @event)
            await this.Publish(@event, cancellationToken).ConfigureAwait(false);

        return execution;
    }

    
    public RequestResult<IAsyncEnumerable<TResult>> RequestWithContext<TResult>(
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken = default,
        params IEnumerable<(string Key, object Value)> headers
    ) => requestExecutor.RequestWithContext(request, cancellationToken, headers);

    
    public Task<CommandContext<TCommand>> Send<TCommand>(
        TCommand request, 
        CancellationToken cancellationToken = default, 
        params IEnumerable<(string Key, object Value)> headers
    ) where TCommand : ICommand => commandExecutor.Send(request, cancellationToken, headers);
    

    public Task<EventAggregatedContext<TEvent>> Publish<TEvent>(
        TEvent @event, 
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        params IEnumerable<(string Key, object Value)> headers
    ) where TEvent : IEvent => eventExecutor.Publish(@event, cancellationToken, executeInParallel, headers);

    
    public IDisposable Subscribe<TEvent>(Func<TEvent, EventContext<TEvent>, CancellationToken, Task> action) where TEvent : IEvent
        => eventExecutor.Subscribe(action);
}
// public async Task<MediatorResult> Request<TResult>(
//     IRequest<TResult> request,
//     CancellationToken cancellationToken = default,
//     params IEnumerable<(string Key, object Value)> headers
// )
// {
//     try
//     {
//         var context = await this
//             .RequestWithContext(request, cancellationToken, headers)
//             .ConfigureAwait(false);
//
//         // TODO: this gets me nothing that the context didn't already have... however, I'm returning a loose object, so I can transform
//         // the result now
//         return new MediatorResult(
//             request,
//             context.Result,
//             null,
//             context.Context
//         );
//     }
//     catch (Exception ex)
//     {
//         // TODO: could apply different exception handler allowing Result to set/handled
//         return new MediatorResult(
//             request,
//             null,
//             ex,
//             null // TODO: context is lost and shouldn't be on exceptions
//         );   
//     }
// }
//
// public record MediatorResult(
//     object Contract,
//     object? Result,
//     Exception? Exception,
//     IMediatorContext Context
// );