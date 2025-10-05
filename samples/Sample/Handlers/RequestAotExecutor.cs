using Sample.Contracts;

namespace Tests;

// TODO: for plans around service bus/remote queues, we'll just inject another executor to handle those mappings
// TODO: inject multiples of executors into the mediator implementation
// [MediatorHandler] on the handler registers for AOT and service registration in DI
public class RequestAotExecutor : global::Shiny.Mediator.Infrastructure.RequestExecutor
{
    public override async Task<TResult> Request<TResult>(
        IMediatorContext context, 
        IRequest<TResult> request, 
        CancellationToken cancellationToken
    ) 
    {
        if (request is OfflineRequest p0)
        {
            object result = await this.Execute<OfflineRequest, string>(p0, context, cancellationToken);
            return (TResult)result;
        }
        
        if (request is CacheRequest p1)
        { 
            object result = await this.Execute<CacheRequest, string>(p1, context, cancellationToken);
            return (TResult)result;
        }

        throw new InvalidOperationException("Unknown request type");
    }


    public override bool CanHandle<TResult>(IRequest<TResult> request)
    {
        if (request is OfflineRequest)
            return true;

        if (request is CacheRequest)
            return true;

        return false;
    }
}


public class StreamRequestAotExecutor : global::Shiny.Mediator.Infrastructure.StreamRequestExecutor
{
    public override IAsyncEnumerable<TResult> Request<TResult>(
        IMediatorContext context,
        IStreamRequest<TResult> request,
        CancellationToken cancellationToken
    )
    {
        if (request is TickerRequest p)
        {
            var handle = this.Execute<TickerRequest, string>(context, p, cancellationToken);
            return (IAsyncEnumerable<TResult>)handle;
        }
        throw new InvalidOperationException("Unknown request type");
    }


    public override bool CanRequest<TResult>(IStreamRequest<TResult> request)
    {
        return (request is TickerRequest);
    }
}