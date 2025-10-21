namespace Shiny.Mediator.Benchmarks;


public record NormalRequest : IRequest<int>;
public class NormalRequestHandler : IRequestHandler<NormalRequest, int>
{
    public Task<int> Handle(NormalRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(1);
    }
}


public record FastRequest : IRequest<int>;

[MediatorSingleton]
public class FastRequestHandler : IRequestHandler<FastRequest, int>
{
    public Task<int> Handle(FastRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(1);
    }
}