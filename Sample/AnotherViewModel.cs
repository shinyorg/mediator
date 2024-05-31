namespace Sample;


public class AnotherViewModel(BaseServices services, AppSqliteConnection conn) : ViewModel(services), IEventHandler<MyMessageEvent>
{
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken) =>
        conn.Log("AnotherViewModel", @event);
}