using Sample.Handlers;
using Shiny.Mediator;

namespace Sample.Uno.Presentation;


public partial class MainViewModel(IMediator mediator, INavigator navigator) : ObservableObject
{
    [ObservableProperty] string? offlineResultText;
    [ObservableProperty] string? offlineDate;

    [RelayCommand]
    async Task Offline()
    {
        var context = await mediator.RequestWithContext(new OfflineRequest());
        var offline = context.Context.Offline();

        this.OfflineResultText = context.Result;
        this.OfflineDate = offline?.Timestamp.ToString() ?? "Not Offline Data";
    }


    [RelayCommand]
    Task ErrorTrap() => mediator.Send(new ErrorCommand());
}