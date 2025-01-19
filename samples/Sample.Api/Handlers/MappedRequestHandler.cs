namespace Sample.Api.Handlers;

public record MappedRequest : IRequest<string>;

[SingletonHandler]
public class MappedRequestHandler : IRequestHandler<MappedRequest, string>
{
    public Task<string> Handle(MappedRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Hello world from mapped handler");
    }
}