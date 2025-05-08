using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;

public class MediatorTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ExceptionHandlers_Optional_WithMsftExtDi()
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        // services.AddConfiguration();
        
        services.AddShinyMediator(null, false);
        services.AddSingletonAsImplementedInterfaces<TestCommandHandler>();
        var sp = services.BuildServiceProvider();

        try
        {
            await sp.GetRequiredService<IMediator>().Send(new TestCommand());
        }
        catch (Exception ex) when (ex.Message.Equals(TestCommandHandler.CrashMessage))
        {
            // test will pass
        }
    }


    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Singleton)]
    public async Task ExceptionHandlers_Work_WithMsftExtDi(ServiceLifetime exceptionHandlerLifetime)
    {
        var services = new ServiceCollection();
        services.AddXUnitLogging(output);
        // services.AddConfiguration();
        
        services.AddShinyMediator(x => x.AddExceptionHandler<TestExceptionHandler>(exceptionHandlerLifetime), false);
        services.AddSingletonAsImplementedInterfaces<TestCommandHandler>();
        var sp = services.BuildServiceProvider();

        await sp.GetRequiredService<IMediator>().Send(new TestCommand());
    }
}


file record TestCommand : ICommand;
file class TestCommandHandler : ICommandHandler<TestCommand>
{
    public const string CrashMessage = "HANDLER BOOM";
    public Task Handle(TestCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException(CrashMessage);
    }
}

file class TestExceptionHandler : IExceptionHandler
{
    public Task<bool> Handle(IMediatorContext context, Exception exception) => Task.FromResult(true);
}