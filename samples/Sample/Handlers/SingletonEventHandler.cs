using Sample.Contracts;

namespace Sample.Handlers;


[SingletonHandler]
public class SingletonEventHandler(IMediator mediator, AppSqliteConnection data) : IEventHandler<MyMessageEvent>
{
    public async Task Handle(MyMessageEvent @event, EventContext context, CancellationToken cancellationToken)
    {
        var random = new Random();
        var wait = random.Next(500, 5000);
        await Task.Delay(wait, cancellationToken);
        
        await data.Log("SingletonEventHandler", @event);
    }
}