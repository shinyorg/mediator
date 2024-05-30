namespace Sample;


public class TriggerViewModel : ViewModel, IEventHandler<MyMessageEvent>
{
    readonly IDisposable sub;
    CancellationTokenSource? cancelSource;
    
    public TriggerViewModel(
        BaseServices services, 
        IMediator mediator,
        AppSqliteConnection data
    ) 
    : base(services)
    {
        // TODO: request without response
        this.TriggerCommand = ReactiveCommand.CreateFromTask(
            async () =>
            {
                this.cancelSource = new();
                var request = new MyMessageRequest(
                    this.Arg!,
                    this.FireAndForgetEvents,
                    this.ParallelEvents
                );
                var response = await mediator.Send(request, this.cancelSource.Token);
                await data.Log(
                    "TriggerViewModel-Response",
                    new MyMessageEvent(
                        response.Response, 
                        this.FireAndForgetEvents, 
                        this.ParallelEvents
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
                        this.Arg, 
                        this.FireAndForgetEvents, 
                        this.ParallelEvents
                    )
                );
            },
            this.WhenAny(
                x => x.IsBusy,
                busy => busy.GetValue()
            )
        );
        
        this.sub = mediator.Subscribe((MyMessageEvent @event, CancellationToken _) =>
            data.Log("TriggerViewModel-Subscribe", @event)
        );
    }

    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something async here
        Console.WriteLine("Scoped Handler: " + @event.Arg);
        return Task.CompletedTask;
    }

    public ICommand TriggerCommand { get; }
    public ICommand CancelCommand { get; }
    [Reactive] public string Arg { get; set; }
    [Reactive] public bool FireAndForgetEvents { get; set; }
    [Reactive] public bool ParallelEvents { get; set; }

    public override void Destroy()
    {
        base.Destroy();
        this.sub?.Dispose();
    }
}