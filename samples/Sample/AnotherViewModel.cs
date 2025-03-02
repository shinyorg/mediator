using CommunityToolkit.Mvvm.ComponentModel;
using Sample.Contracts;

namespace Sample;


public partial class AnotherViewModel(AppSqliteConnection conn) : ObservableObject, INavigatedAware, IEventHandler<MyMessageEvent>
{
    const string Key = nameof(MyPrismNavCommand);

    
    public void OnNavigatedFrom(INavigationParameters parameters) {}
    public void OnNavigatedTo(INavigationParameters parameters)
    {
        if (parameters.ContainsKey(Key))
        {
            var p = parameters.GetValue<MyPrismNavCommand>(Key);

            this.ShowArg = true;
            this.Arg = p.Arg ?? "No Argument";
        }
    }


    [ObservableProperty] bool showArg;
    [ObservableProperty] string arg;

    [MainThread]
    public Task Handle(MyMessageEvent @event, IMediatorContext context, CancellationToken cancellationToken) =>
        conn.Log("AnotherViewModel", @event);
}