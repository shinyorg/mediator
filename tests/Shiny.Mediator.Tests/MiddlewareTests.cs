namespace Shiny.Mediator.Tests;

public class MiddlewareTests
{
    // TODO: test execution chain
    public MiddlewareTests()
    {
        Executed.Constrained = false;
        Executed.Variant = false;
    }
    
    
    public async Task Constrained()
    {
        
    }


    [Fact]
    public async Task ConstrainedAndOpen()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator();
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResponseHandler>();
        services.AddSingletonAsImplementedInterfaces<ConstrainedMiddleware>();
        services.AddSingleton(typeof(IRequestMiddleware<,>), typeof(VariantRequestMiddleware<,>));
        var sp = services.BuildServiceProvider();
        
        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Request(new MiddlewareResponseRequest());
        Executed.Constrained.Should().BeTrue();
        Executed.Variant.Should().BeTrue();
    }
}


public class MiddlewareRequest : IRequest;

public class MiddlewareResponseRequest : IRequest<int>;


public class MiddlewareRequestHandler : IRequestHandler<MiddlewareRequest>
{
    public Task Handle(MiddlewareRequest request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class MiddlewareRequestResponseHandler : IRequestHandler<MiddlewareResponseRequest, int>
{
    public Task<int> Handle(MiddlewareResponseRequest request, CancellationToken cancellationToken)
        => Task.FromResult(1);
}

public static class Executed
{
    // TODO: need to know execution order
    public static bool Constrained { get; set; }
    public static bool Variant { get; set; }
}
public class ConstrainedMiddleware : IRequestMiddleware<MiddlewareResponseRequest, int>
{
    public Task<int> Process(MiddlewareResponseRequest request, Func<Task<int>> next, CancellationToken cancellationToken)
    {
        Executed.Constrained = true;
        return next();
    }
}

public class VariantRequestMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Process(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        Executed.Variant = true;
        return next();
    }
}