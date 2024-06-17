namespace Shiny.Mediator.Tests;


public class RequestHandlerTests
{
}


public record TestResultRequest(string Arg) : IRequest<string>;

public class TestResultRequestHandler : IRequestHandler<TestResultRequest, string>
{
    public Task<string> Handle(TestResultRequest request, CancellationToken cancellationToken)
        => Task.FromResult("RESPONSE-" + request.Arg);
}