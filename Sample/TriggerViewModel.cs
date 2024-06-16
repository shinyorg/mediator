using Sample.Contracts;

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
                        this.Arg, 
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
        
        this.sub = mediator.Subscribe((MyMessageEvent @event, CancellationToken _) =>
            data.Log("TriggerViewModel-Subscribe", @event)
        );
    }

    
    [MainThread]
    public Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something async here
        Console.WriteLine("Scoped Handler: " + @event.Arg);
        return Task.CompletedTask;
    }

    public ICommand ErrorTrap { get; }
    public ICommand TriggerCommand { get; }
    public ICommand CancelCommand { get; }
    [Reactive] public string Arg { get; set; }
    [Reactive] public bool FireAndForgetEvents { get; set; }

    public override void Destroy()
    {
        base.Destroy();
        this.sub?.Dispose();
    }
}