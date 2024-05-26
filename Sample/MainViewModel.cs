using Sample.Handlers.MyMessage;

namespace Sample;

// scoped
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
        this.TriggerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            this.cancelSource = new();
            var response = await mediator.Send(new MyMessageRequest(this.Arg!), this.cancelSource.Token);
            Console.WriteLine("RESPONSE: " + response.Response);
        });

        this.CancelCommand = ReactiveCommand.Create(() => this.cancelSource?.Cancel());
        
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
}