using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Shiny.Mediator.Tests;


public class CommandHandlerTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Missing_CommandHandler()
    {
        try
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddXUnit(output));
            
            services.AddShinyMediator();
            var sp = services.BuildServiceProvider();
            var mediator = sp.GetRequiredService<IMediator>();
            await mediator.Send(new TestCommand());
            Assert.Fail("This should not have passed");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.ShouldBe("No command handler found for Shiny.Mediator.Tests.TestCommand");
        }
    }
}


public class TestCommand : ICommand
{
    public int Delay { get; set; }
}

public class Test1CommandHandler : ICommandHandler<TestCommand>
{
    public async Task Handle(TestCommand command, IMediatorContext context, CancellationToken cancellationToken)
    {
        if (command.Delay > 0)
            await Task.Delay(command.Delay);
    }
}