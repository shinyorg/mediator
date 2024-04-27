using Shiny.Mediator;
using Shiny.Mediator.Contracts;

namespace Sample.Handlers.MyMessage;

public class SingletonEventHandler : IEventHandler<MyMessage>
{
    public async Task Handle(MyMessage @event, CancellationToken cancellationToken)
    {
        // do something
    }
}