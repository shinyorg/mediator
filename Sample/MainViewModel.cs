namespace Sample;


public class MainViewModel : ViewModel, IEventHandler<MyMessageEvent>
{
    readonly IDisposable sub;
    CancellationTokenSource? cancelSource;
    
    public MainViewModel(BaseServices services, IMediator mediator) : base(services)
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
                Console.WriteLine("RESPONSE: " + response.Response);
            }
        );
        this.BindBusyCommand(this.TriggerCommand);

        this.CancelCommand = ReactiveCommand.Create(
            () => this.cancelSource?.Cancel(),
            this.WhenAny(
                x => x.IsBusy,
                busy => busy.GetValue()
            )
        );
        
        this.sub = mediator.Subscribe(async (MyMessageEvent @event, CancellationToken ct) =>
        {
            // do something async here
            Console.WriteLine("Message Subscribe: " + @event.Arg);
        });
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
}