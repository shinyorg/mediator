namespace Shiny.Mediator;

public class StreamExecutionContext(CancellationToken cancellationToken) //IStreamRequestHandler<>)
{
    public Guid ExecutionId { get; }= Guid.NewGuid();
    public CancellationToken CancellationToken => cancellationToken;
    
    // TODO: for each pump, I can set a execution #
        // TODO: pump 1 is from cache
        // TODO: pump 2 is from offline
        // TODO: pump 3 is from live
}