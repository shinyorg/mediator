namespace Shiny.Mediator.Tests;

public class MiddlewareTests
{
    // TODO: test execution chain
    public MiddlewareTests()
    {
        Executed.Constrained = false;
        Executed.Variant = false;
    }
    
    
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConstrainedAndOpen(bool addConstrained, bool addOpen)
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(cfg =>
        {
            
        });
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResultHandler>();
        
        if (addConstrained)
            services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, ConstrainedMiddleware>();
            //services.AddSingletonAsImplementedInterfaces<ConstrainedMiddleware>();
        
        if (addOpen)
            services.AddSingleton(typeof(IRequestMiddleware<,>), typeof(VariantRequestMiddleware<,>));
        
        var sp = services.BuildServiceProvider();
        
        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Request(new MiddlewareResultRequest());
        Executed.Constrained.Should().Be(addConstrained);
        Executed.Variant.Should().Be(addOpen);
    }
}


public class MiddlewareRequest : IRequest;

public class MiddlewareResultRequest : IRequest<int>;


// public class MiddlewareRequestHandler : IRequestHandler<MiddlewareRequest>
// {
//     public Task Handle(MiddlewareRequest request, CancellationToken cancellationToken)
//         => Task.CompletedTask;
// }

public class MiddlewareRequestResultHandler : IRequestHandler<MiddlewareResultRequest, int>
{
    public Task<int> Handle(MiddlewareResultRequest request, CancellationToken cancellationToken)
        => Task.FromResult(1);
}

public static class Executed
{
    // TODO: need to know execution order
    public static bool Constrained { get; set; }
    public static bool Variant { get; set; }
}
public class ConstrainedMiddleware : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(MiddlewareResultRequest request, Func<Task<int>> next, CancellationToken cancellationToken)
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