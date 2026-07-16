using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Mediator.Infrastructure.Impl;


/// <summary>
/// Default <see cref="IMediator"/> implementation. Creates a service scope and root
/// <see cref="IMediatorContext"/> for each dispatch, defers to the <see cref="IMediatorDirector"/>
/// to pick an executor, and routes thrown exceptions through the registered <see cref="IExceptionHandler"/> chain.
/// </summary>
public class MediatorImpl(
    ILogger<MediatorImpl> logger,
    IServiceProvider services,
    IMediatorDirector director
) : IMediator
{
    /// <inheritdoc/>
    public async Task<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IRequest<TResult> request, 
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        TResult result = default!;
        
        using var scope = services.CreateScope();
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        var context = new MediatorContext(scope, request, activity, director);
        configure?.Invoke(context);
        try
        {
            result = await director
                .GetRequestExecutor(request)
                .Request(
                    context,
                    request,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (result is IEvent @event)
            {
                logger.LogDebug("Event Returned by Request - Publishing: {EventType}", @event.GetType().FullName);
                var child = context.CreateChild(@event, true);
                await director
                    .GetEventExecutor(@event)
                    .Publish(child, @event, true, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            var handled = await this
                .TryHandle(context, exception)
                .ConfigureAwait(false);

            if (!handled)
                throw;
        }
        return (context, result);
    }


    /// <inheritdoc/>
    public async IAsyncEnumerable<(IMediatorContext Context, TResult Result)> Request<TResult>(
        IStreamRequest<TResult> request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    )
    {
        using var scope = services.CreateScope();
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, request, activity, director);
        configure?.Invoke(context);
        var enumerable = director
            .GetStreamRequestExecutor(request)
            .Request(context, request, cancellationToken);

        await foreach (var result in enumerable)
        {
            yield return (context, result);
        }
    }


    /// <inheritdoc/>
    public async Task<IMediatorContext> Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default,
        Action<IMediatorContext>? configure = null
    ) where TCommand : ICommand
    {
        using var scope = services.CreateScope();
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, command, activity, director);
        configure?.Invoke(context);
        
        try
        {
            await director
                .GetCommandExecutor(command)
                .Send(context, command, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var handled = await this
                .TryHandle(context, exception)
                .ConfigureAwait(false);
            
            if (!handled)
                throw;
        }

        return context;
    }


    /// <inheritdoc/>
    public async Task<IMediatorContext> Publish<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        using var scope = services.CreateScope();
        using var activity = MediatorActivitySource.Value.StartActivity()!;
        
        var context = new MediatorContext(scope, @event, activity, director);
        configure?.Invoke(context);
        
        try
        {
            await director
                .GetEventExecutor(@event)
                .Publish(context, @event, executeInParallel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.Exception = exception;
            
            var handled = await this.TryHandle(context, exception).ConfigureAwait(false);
            if (!handled)
                throw;
        }
        return context;
    }

    
    /// <inheritdoc/>
    public void PublishToBackground<TEvent>(
        TEvent @event,
        bool executeInParallel = true,
        Action<IMediatorContext>? configure = null
    ) where TEvent : IEvent
    {
        var scope = services.CreateScope();
        var activity = MediatorActivitySource.Value.StartActivity()!;

        var context = new MediatorContext(scope, @event, activity, director);
        configure?.Invoke(context);

        _ = Task.Run(async () =>
        {
            try
            {
                await director
                    .GetEventExecutor(@event)
                    .Publish(context, @event, executeInParallel, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await this.TryHandle(context, ex).ConfigureAwait(false);
            }
            finally
            {
                activity.Dispose();
                scope.Dispose();
            }
        });
    }
    


    /// <inheritdoc/>
    public IDisposable Subscribe<TEvent>(Func<TEvent, IMediatorContext, CancellationToken, Task> action) where TEvent : IEvent
        => director.GetEventExecutor<TEvent>().Subscribe(action);


    Task<bool> TryHandle(MediatorContext context, Exception exception)
        => MediatorExceptionHandling.TryHandle(context, exception, logger);
}