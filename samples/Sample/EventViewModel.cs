using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample;


public partial class EventViewModel(
    IPageDialogService dialogs,
    AppSqliteConnection conn
) : ObservableObject, IPageLifecycleAware
{
    [RelayCommand]
    async Task Clear()
    {
        var confirm = await dialogs.DisplayAlertAsync("Clear all events?", "Confirm", "Yes", "No");
        if (confirm)
        {
            await conn.DeleteAllAsync<LogModel>();
            await this.LoadCommand.ExecuteAsync(null!);
        }
    }


    [RelayCommand]
    async Task Load()
    {
        var tmp = await conn.Logs.OrderByDescending(x => x.Timestamp).ToListAsync();
        this.List = tmp
            .Select(x => new EventItemViewModel(
                x.Area,
                x.Arg,
                x.FireAndForget,
                x.ExecutionTimeMillis,
                x.Timestamp.ToLocalTime().ToString("g")
            ))
            .ToList();
    }

    [ObservableProperty] List<EventItemViewModel> list;
    [ObservableProperty] bool isBusy;
    
    public void OnAppearing() => this.LoadCommand.Execute(null);
    public void OnDisappearing() {}
}

public record EventItemViewModel(
    string Area,
    string Arg,
    bool FireAndForget,
    long ElapsedMillis,
    string Timestamp
);