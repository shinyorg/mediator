using Sample.Handlers.MyMessage;

namespace Sample;


public class MainViewModel : ViewModel
{
    readonly IDisposable sub;
    CancellationTokenSource? cancelSource;
    
    public MainViewModel(
        BaseServices services, 
        IMediator mediator,
        MediatorEventHandler<MyMessageEvent> scopedHandler
    ) : base(services)
    {
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
        
        scopedHandler.OnHandle = async (@event, ct) =>
        {
            // do something async here
            Console.WriteLine("Scoped Handler: " + @event.Arg);
        };
        
        this.sub = mediator.Subscribe(async (MyMessageEvent @event, CancellationToken ct) =>
        {
            // do something async here
            Console.WriteLine("Message Subscribe: " + @event.Arg);
        });
    }


    public ICommand TriggerCommand { get; }
    public ICommand CancelCommand { get; }
    [Reactive] public string Arg { get; set; }
    [Reactive] public bool FireAndForgetEvents { get; set; }
    [Reactive] public bool ParallelEvents { get; set; }
}