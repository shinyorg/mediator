namespace Sample.Handlers.MyMessage;

public class SingletonEventHandler : IEventHandler<MyMessageEvent>
{
    public async Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something
    }
}