using Sample.Handlers;
using Shiny.Mediator;

namespace Sample.Uno.Presentation;


public partial class MainViewModel(
    IMediator mediator, 
    INavigator navigator
) : ObservableObject, IEventHandler<AppEvent>
{
    [ObservableProperty] string offlineResultText = "No Result";
    [ObservableProperty] string offlineDate = "Not Offline";

    [RelayCommand]
    async Task Offline()
    {
        var context = await mediator
            .RequestWithContext(new OfflineRequest())
            .ConfigureAwait(true);
        var offline = context.Context.Offline();

        this.OfflineResultText = context.Result;
        this.OfflineDate = offline?.Timestamp.ToString() ?? "Not Offline Data";
    }

    [RelayCommand]
    async Task PublishEvent()
    {
        await mediator.Publish(new AppEvent("Hello from SecondViewModel"));
        await navigator.ShowMessageDialogAsync(this, title: "Done", content: "Publish message sent successfully");
    }

    [RelayCommand]
    Task ErrorTrap() => mediator.Send(new ErrorCommand());
    
    [RelayCommand]
    Task GoToSecondPage() => navigator.NavigateViewModelAsync<SecondViewModel>(this);

    public async Task Handle(AppEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine(@event.Message);
    }
}