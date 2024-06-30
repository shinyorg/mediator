using Microsoft.Extensions.Caching.Memory;
using Sample.Contracts;

namespace Sample;


public class TriggerViewModel : ViewModel, IEventHandler<MyMessageEvent>
{
    readonly IDisposable sub;
    CancellationTokenSource? cancelSource;
    
    public TriggerViewModel(
        BaseServices services, 
        IMediator mediator,
        IMemoryCache cache,
        AppSqliteConnection data
    ) 
    : base(services)
    {
        this.TriggerCommand = ReactiveCommand.CreateFromTask(
            async () =>
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
                await this.Dialogs.Alert("Execution Complete");
            }
        );
        this.BindBusyCommand(this.TriggerCommand);

        this.CancelCommand = ReactiveCommand.CreateFromTask(
            async () =>
            {
                this.cancelSource?.Cancel();
                await data.Log(
                    "TriggerViewModel-Cancel",
                    new MyMessageEvent(
                        this.Arg!, 
                        this.FireAndForgetEvents
                    )
                );
            },
            this.WhenAny(
                x => x.IsBusy,
                busy => busy.GetValue()
            )
        );

        this.ErrorTrap = ReactiveCommand.CreateFromTask(() => mediator.Send(new ErrorRequest()));

        this.Stream = ReactiveCommand.CreateFromTask(async () =>
        {
            var stream = mediator.Request(
                new TickerRequest(this.StreamRepeat, this.StreamMultiplier, this.StreamGapSeconds), 
                this.DeactivateToken
            );
            await foreach (var item in stream)
            {
                this.StreamLastResponse = item;
            } 
        });
        this.sub = mediator.Subscribe((MyMessageEvent @event, CancellationToken _) =>
            data.Log("TriggerViewModel-Subscribe", @event)
        );

        this.CacheClear = ReactiveCommand.CreateFromTask(async () =>
        {
            cache.Clear();
            await this.Dialogs.Alert("Cache Cleared");
        });

        this.CancelStream = ReactiveCommand.CreateFromTask(async () =>
        {
            this.Deactivate();
            await this.Dialogs.Alert("All streams cancelled");
        });
        this.CacheRequest = ReactiveCommand.CreateFromTask(async () =>
        {
            this.CacheValue = await mediator.Request(new CacheRequest());
        });

        this.ResilientCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            this.ResilientValue = await mediator.Request(new ResilientRequest());
        });

        this.OfflineCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            this.OfflineValue = await mediator.Request(new OfflineRequest());
        });

        this.RefreshTimerStart = ReactiveCommand.CreateFromTask(async () =>
        {
            var en = mediator.Request(new AutoRefreshRequest(), this.DeactivateToken).GetAsyncEnumerator(this.DeactivateToken);
            while (await en.MoveNextAsync())
            {
                await MainThread.InvokeOnMainThreadAsync(
                    () => this.LastRefreshTimerValue = en.Current
                );
            }
        });

        this.PrismNav = ReactiveCommand.CreateFromTask(async () =>
        {
            await mediator.Send(new MyPrismNavRequest(this.PrismNavArg!), CancellationToken.None);
        });
    }

    
    [MainThread]
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something async here
        Console.WriteLine("Scoped Handler: " + @event.Arg);
        return Task.CompletedTask;
    }

    
    public ICommand PrismNav { get; }
    [Reactive] public string PrismNavArg { get; set; }
    
    public ICommand OfflineCommand { get; }
    [Reactive] public string OfflineValue { get; private set; }
    
    public ICommand CancelStream { get; }
    public ICommand ErrorTrap { get; }
    public ICommand TriggerCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CacheRequest { get; }
    public ICommand CacheClear { get; }
    [Reactive] public string CacheValue { get; private set; }

    [Reactive] public string LastRefreshTimerValue { get; private set; }
    public ICommand RefreshTimerStart { get; }
    
    public ICommand ResilientCommand { get; }
    [Reactive] public string ResilientValue { get; private set; }
    
    [Reactive] public string Arg { get; set; }
    [Reactive] public bool FireAndForgetEvents { get; set; }

    public ICommand Stream { get; }
    [Reactive] public int StreamGapSeconds { get; set; } = 1;
    [Reactive] public int StreamRepeat { get; set; } = 5;
    [Reactive] public int StreamMultiplier { get; set; } = 2;
    [Reactive] public string? StreamLastResponse { get; private set; } 
    
    public override void Destroy()
    {
        base.Destroy();
        this.sub?.Dispose();
    }
}