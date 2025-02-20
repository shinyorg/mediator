using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Mediator.Infrastructure.Impl;


public partial class Mediator(
    IServiceProvider services,
    IEnumerable<IEventCollector> collectors
) : IMediator
{
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
}