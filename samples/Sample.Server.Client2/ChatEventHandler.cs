using Sample.Server.Contracts;
using Shiny.Mediator;

namespace Sample.Server.Client2;


[SingletonHandler]
public class ChatEventHandler : IEventHandler<ChatEvent>
{
    public Task Handle(ChatEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{@event.From}]: {@event.Message}");
        return Task.CompletedTask;
    }
}