namespace Shiny.Mediator.Tests;

public class MiddlewareTests
{
    public MiddlewareTests()
    {
        Executed.Constrained = false;
        Executed.Variant = false;
        ExecutionTracker.Clear();
    }


    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task ConstrainedAndOpen(bool addConstrained, bool addOpen)
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddLogging();
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResultHandler>();

        if (addConstrained)
            services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, ConstrainedMiddleware>();

        if (addOpen)
            services.AddSingleton(typeof(IRequestMiddleware<,>), typeof(VariantRequestMiddleware<,>));

        var sp = services.BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Request(new MiddlewareResultRequest());
        Executed.Constrained.ShouldBe(addConstrained);
        Executed.Variant.ShouldBe(addOpen);
    }


    [Fact]
    public async Task ExplicitOrderOverridesDIRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddLogging();
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResultHandler>();

        // Register in order: C(3), A(1), B(2) - should execute as A, B, C
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareC>();
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareA>();
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareB>();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Request(new MiddlewareResultRequest());

        ExecutionTracker.Order.Count.ShouldBe(3);
        ExecutionTracker.Order[0].ShouldBe("A");
        ExecutionTracker.Order[1].ShouldBe("B");
        ExecutionTracker.Order[2].ShouldBe("C");
    }


    [Fact]
    public async Task MixedOrderedAndUnorderedMiddleware()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddLogging();
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResultHandler>();

        // OrderedMiddlewareA has order 1, UnorderedMiddleware has no attribute (default 0), OrderedMiddlewareNeg has order -1
        // Expected execution: Neg(-1), Unordered(0), A(1)
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareA>();
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, UnorderedMiddleware>();
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareNeg>();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Request(new MiddlewareResultRequest());

        ExecutionTracker.Order.Count.ShouldBe(3);
        ExecutionTracker.Order[0].ShouldBe("Neg");
        ExecutionTracker.Order[1].ShouldBe("Unordered");
        ExecutionTracker.Order[2].ShouldBe("A");
    }


    [Fact]
    public async Task SameOrderPreservesDIRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddShinyMediator(includeStandardMiddleware: false);
        services.AddLogging();
        services.AddSingletonAsImplementedInterfaces<MiddlewareRequestResultHandler>();

        // Both have order 1, registered A then A2 - should execute A then A2 (stable sort)
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareA>();
        services.AddSingleton<IRequestMiddleware<MiddlewareResultRequest, int>, OrderedMiddlewareA2>();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();
        await mediator.Request(new MiddlewareResultRequest());

        ExecutionTracker.Order.Count.ShouldBe(2);
        ExecutionTracker.Order[0].ShouldBe("A");
        ExecutionTracker.Order[1].ShouldBe("A2");
    }
}


public class MiddlewareResultRequest : IRequest<int>;

public class MiddlewareRequestResultHandler : IRequestHandler<MiddlewareResultRequest, int>
{
    public Task<int> Handle(MiddlewareResultRequest request, IMediatorContext context, CancellationToken cancellationToken)
        => Task.FromResult(1);
}

public static class Executed
{
    public static bool Constrained { get; set; }
    public static bool Variant { get; set; }
}

public static class ExecutionTracker
{
    public static List<string> Order { get; } = new();
    public static void Clear() => Order.Clear();
}

public class ConstrainedMiddleware : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        Executed.Constrained = true;
        return next();
    }
}

public class VariantRequestMiddleware<TRequest, TResult> : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public Task<TResult> Process(IMediatorContext context, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        Executed.Variant = true;
        return next();
    }
}

[MiddlewareOrder(1)]
public class OrderedMiddlewareA : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("A");
        return next();
    }
}

[MiddlewareOrder(1)]
public class OrderedMiddlewareA2 : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("A2");
        return next();
    }
}

[MiddlewareOrder(2)]
public class OrderedMiddlewareB : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("B");
        return next();
    }
}

[MiddlewareOrder(3)]
public class OrderedMiddlewareC : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("C");
        return next();
    }
}

[MiddlewareOrder(-1)]
public class OrderedMiddlewareNeg : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("Neg");
        return next();
    }
}

public class UnorderedMiddleware : IRequestMiddleware<MiddlewareResultRequest, int>
{
    public Task<int> Process(IMediatorContext context, RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        ExecutionTracker.Order.Add("Unordered");
        return next();
    }
}
