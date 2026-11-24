using CommunityToolkit.Mvvm.ComponentModel;
using Sample.Contracts;

namespace Sample;


public partial class AnotherViewModel(
    AppSqliteConnection conn
) : ObservableObject, IQueryAttributable, IEventHandler<MyMessageEvent>
{
    public const string ArgKey = "Arg";


    // Shell hands the navigation arguments from MyNavCommandHandler to the page's binding context
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ArgKey, out var arg))
        {
            this.ShowArg = true;
            this.Arg = arg as string ?? "No Argument";
        }
    }


    [ObservableProperty] bool showArg;
    [ObservableProperty] string arg;

    [MainThread]
    public Task Handle(MyMessageEvent @event, IMediatorContext context, CancellationToken cancellationToken) =>
        conn.Log("AnotherViewModel", @event);
}