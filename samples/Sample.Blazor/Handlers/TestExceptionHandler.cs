using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Sample.Blazor.Handlers;


public class TestExceptionHandler(IAlertDialogService alerts) : IExceptionHandler
{
    public async Task<bool> Handle(IMediatorContext context, Exception exception)
    {
        var cmd = context.Message as ErrorCommand;
        if (cmd?.HandleIt == true)
        {
            alerts.Display("Error", "What did you do?");
            return true;
        }

        return false;
    }
}