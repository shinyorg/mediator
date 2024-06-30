using Sample.Contracts;
using Shiny.Mediator.Middleware;

namespace Sample;


public class AnotherViewModel(BaseServices services, AppSqliteConnection conn) : ViewModel(services), IEventHandler<MyMessageEvent>
{
    const string Key = nameof(MyPrismNavRequest);
    
    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);
        if (parameters.ContainsKey(Key))
        {
            var p = parameters.GetValue<MyPrismNavRequest>(Key);

            this.ShowArg = true;
            this.Arg = p.Arg ?? "No Argument";
        }
    }
    
    
    [Reactive] public bool ShowArg { get; private set; }
    [Reactive] public string Arg { get; private set; }

    [MainThread]
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken) =>
        conn.Log("AnotherViewModel", @event);
}