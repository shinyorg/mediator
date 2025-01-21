using Sample.Server.Contracts;
using Shiny.Mediator;

namespace Sample.Server.Client1;


[SingletonHandler]
public class ChatEventHandler : IEventHandler<ChatEvent>
{
    public Task Handle(ChatEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{@event.From}]: {@event.Message}");
        return Task.CompletedTask;
    }
}