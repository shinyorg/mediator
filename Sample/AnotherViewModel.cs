using Sample.Contracts;
using Shiny.Mediator.Middleware;

namespace Sample;


public class AnotherViewModel(BaseServices services, AppSqliteConnection conn) : ViewModel(services), IEventHandler<MyMessageEvent>
{
    [MainThread]
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken) =>
        conn.Log("AnotherViewModel", @event);
}