namespace Sample.Api.Handlers;

public record TestResultRequest(int Number) : IRequest<TestResult>;
public record TestResult;


[ScopedHandler]
[MediatorHttpPost("TestResult", "/testresult")]
public class TestResultRequestHandler : IRequestHandler<TestResultRequest, TestResult>
{
    public Task<TestResult> Handle(TestResultRequest request, RequestContext<TestResultRequest> context, CancellationToken cancellationToken)
        => Task.FromResult(new TestResult());
}