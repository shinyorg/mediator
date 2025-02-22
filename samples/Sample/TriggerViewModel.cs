using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Sample.Contracts;
using Sample.ThemeParksApi;

namespace Sample;


public partial class TriggerViewModel(
    IMediator mediator,
    AppSqliteConnection data,
    IPageDialogService dialogs
) : ObservableObject, IEventHandler<MyMessageEvent>, IEventHandler<ConnectivityChanged>
{
    readonly IDisposable sub;
    CancellationTokenSource cancelSource = new();
    

        // this.sub = mediator.Subscribe((MyMessageEvent @event, CancellationToken _) =>
        //     data.Log("TriggerViewModel-Subscribe", @event)
        // );
        
    
    [MainThread]
    public Task Handle(MyMessageEvent @event, EventContext<MyMessageEvent> context, CancellationToken cancellationToken)
    {
        // do something async here
        Console.WriteLine("Scoped Handler: " + @event.Arg);
        return Task.CompletedTask;
    }


    [RelayCommand]
    Task PrismNav()
        => mediator.Send(new MyPrismNavCommand(this.PrismNavArg!), CancellationToken.None);
    
    [ObservableProperty] string prismNavArg;


    [RelayCommand]
    async Task Offline()
    {
        var context = await mediator.RequestWithContext(new OfflineRequest());
        this.OfflineValue = context.Result;
        this.OfflineTimestamp = context.Context.Offline()?.Timestamp;
    }
    
    [ObservableProperty] string offlineValue;
    [ObservableProperty] DateTimeOffset? offlineTimestamp;

    
    [RelayCommand]
    Task ErrorTrap() => mediator.Send(new ErrorCommand());


    [RelayCommand]
    async Task Trigger()
    {
        this.cancelSource = new();
        var request = new MyMessageRequest(
            this.Arg!,
            this.FireAndForgetEvents
        );
        var result = await mediator.Request(request, this.cancelSource.Token);
        
        await data.Log(
            "TriggerViewModel-Response",
            new MyMessageEvent(
                result.Response, 
                this.FireAndForgetEvents 
            )
        );
        await dialogs.DisplayAlertAsync("Execution Complete", "DONE", "Ok");
    }

    [RelayCommand]
    async Task Cancel()
    {
        this.cancelSource.Cancel();
        await dialogs.DisplayAlertAsync("All streams cancelled", "DONE", "Ok");
        this.cancelSource = new();
    }


    [RelayCommand]
    async Task HttpRequest()
    {
        try
        {
            var result = await mediator.Request(new GetDestinationsHttpRequest(), this.cancelSource.Token);
            await dialogs.DisplayAlertAsync("Results", result.Destinations.Count.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK");
        }
    }

    
    [RelayCommand]
    async Task CacheRequest()
    {
        var context = await mediator.RequestWithContext(new CacheRequest());
        this.CacheValue = context.Result;
        this.CacheTimestamp = context.Context.Cache()?.Timestamp;
    }


    [RelayCommand]
    async Task CacheClear()
    {
        await mediator.Publish(new FlushAllStoresEvent());
        await dialogs.DisplayAlertAsync("Cache Cleared", "DONE", "Ok");
    }
    [ObservableProperty] string cacheValue;
    [ObservableProperty] DateTimeOffset? cacheTimestamp;
    [ObservableProperty] string lastRefreshTimerValue;

    [RelayCommand]
    async Task NoHandler()
    {
        try
        {
            await mediator.Send(new NoHandlerCommand());
            await dialogs.DisplayAlertAsync("NO HANDLER", "You should not be here", "OK");
        }
        catch (Exception ex)
        {
            await dialogs.DisplayAlertAsync("EXPECTED", ex.ToString(), "OK");
        }
    }

    [RelayCommand]
    async Task RefreshTimerStart()
    {
        try
        {
            var en = mediator
                .Request(new AutoRefreshRequest(), this.cancelSource.Token)
                .GetAsyncEnumerator(this.cancelSource.Token);

            while (await en.MoveNextAsync())
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => this.LastRefreshTimerValue = en.Current
                );
            }
        }
        catch (TaskCanceledException) { }
    }

    
    [RelayCommand]
    async Task Validate()
    {
        try
        {
            await mediator.Send(new MyValidateCommand { Url = this.ValidateUrl });
            await dialogs.DisplayAlertAsync("All is good", "VALID", "Ok");
        }
        catch (ValidateException ex)
        {
            this.ValidateError = ex.Result.Errors.First().Value.First();
        }
    }
    [ObservableProperty] string validateUrl;
    [ObservableProperty] string validateError;


    [RelayCommand]
    async Task Resilient() => this.ResilientValue = await mediator.Request(new ResilientRequest());
    [ObservableProperty] string resilientValue;

    
    [ObservableProperty] string arg;
    [ObservableProperty] bool fireAndForgetEvents;

    [RelayCommand]
    async Task Stream()
    {
        try
        {
            var stream = mediator.Request(
                new TickerRequest(this.StreamRepeat, this.StreamMultiplier, this.StreamGapSeconds),
                this.cancelSource.Token
            );
            await foreach (var item in stream)
            {
                this.StreamLastResponse = item;
            }
        }
        catch (TaskCanceledException) {}
    }
    
    [ObservableProperty] int streamGapSeconds = 1;
    [ObservableProperty] int streamRepeat = 5;
    [ObservableProperty] int streamMultiplier = 2;
    [ObservableProperty] string? streamLastResponse;
    [ObservableProperty] bool connected;
    [ObservableProperty] string? connectivityChangeTime;
    
    
    [MainThread]
    public Task Handle(ConnectivityChanged @event, EventContext<ConnectivityChanged> context, CancellationToken cancellationToken)
    {
        this.Connected = @event.Connected;
        this.ConnectivityChangeTime = DateTimeOffset.Now.ToString("h:mm:ss tt");
        return Task.CompletedTask;
    }
}