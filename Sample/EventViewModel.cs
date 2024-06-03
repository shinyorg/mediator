namespace Sample;


public class EventViewModel : ViewModel
{
    public EventViewModel(
        BaseServices services, 
        AppSqliteConnection conn
    ) : base(services)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            var list = await conn.Logs.OrderByDescending(x => x.Timestamp).ToListAsync();
            this.List = list
                .Select(x => new EventItemViewModel(
                    x.Area,
                    x.Arg,
                    x.FireAndForget,
                    x.ExecutionTimeMillis,
                    x.Timestamp.ToLocalTime().ToString("g")
                ))
                .ToList();
        });
        this.BindBusyCommand(this.Load);
        
        this.Clear = ReactiveCommand.CreateFromTask(async () =>
        {
            var confirm = await this.Dialogs.Confirm("Clear all events?", "Confirm");
            if (confirm)
            {
                await conn.DeleteAllAsync<LogModel>();
                this.Load.Execute(null);
            }
        });
    }
    
    public ICommand Clear { get; }
    public ICommand Load { get; }
    [Reactive] public List<EventItemViewModel> List { get; private set; }

    public override void OnAppearing()
    {
        base.OnAppearing();
        this.Load.Execute(null);
    }
}

public record EventItemViewModel(
    string Area,
    string Arg,
    bool FireAndForget,
    long ElapsedMillis,
    string Timestamp
);