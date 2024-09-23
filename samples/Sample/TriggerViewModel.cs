using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Http.TheActual;
using Microsoft.Extensions.Caching.Memory;
using Sample.Contracts;

namespace Sample;


public partial class TriggerViewModel(
    IMediator mediator,
    IMemoryCache cache,
    AppSqliteConnection data,
    IPageDialogService dialogs
) : ObservableObject, IEventHandler<MyMessageEvent>
{
    readonly IDisposable sub;
    CancellationTokenSource cancelSource = new();
    

        // this.sub = mediator.Subscribe((MyMessageEvent @event, CancellationToken _) =>
        //     data.Log("TriggerViewModel-Subscribe", @event)
        // );
        
    
    [MainThread]
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something async here
        Console.WriteLine("Scoped Handler: " + @event.Arg);
        return Task.CompletedTask;
    }


    [RelayCommand]
    Task PrismNav()
        => mediator.Send(new MyPrismNavRequest(this.PrismNavArg!), CancellationToken.None);
    
    [ObservableProperty] string prismNavArg;

    
    [RelayCommand]
    async Task Offline() => this.OfflineValue = await mediator.Request(new OfflineRequest());
    [ObservableProperty] string offlineValue;

    [RelayCommand]
    Task ErrorTrap() => mediator.Send(new ErrorRequest());


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
            //await mediator.Request(new TestResultHttpRequest(), this.cancelSource.Token);
        }
        catch (Exception ex)
        {
            await dialogs.DisplayAlertAsync("ERROR", ex.ToString(), "OK");
        }
    }

    [RelayCommand]
    async Task CacheRequest() 
        => this.CacheValue = await mediator.Request(new CacheRequest());
    
    
    [RelayCommand]
    async Task CacheClear()
    {
        cache.Clear();
        await dialogs.DisplayAlertAsync("Cache Cleared", "DONE", "Ok");
    }
    [ObservableProperty] string cacheValue;
    [ObservableProperty] string lastRefreshTimerValue;


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
            await mediator.Send(new MyValidateRequest { Url = this.ValidateUrl });
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
}