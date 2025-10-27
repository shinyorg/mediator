namespace Sample.Api.Handlers;

public record TestResultRequest(int Number) : IRequest<TestResult>;
public record TestResult;


[MediatorScoped]
[MediatorHttpGroup("/test")]
public class TestResultRequestHandler : IRequestHandler<TestResultRequest, TestResult>
{
    [MediatorHttpPost("TestResult", "/result")]
    public Task<TestResult> Handle(TestResultRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(new TestResult());
}