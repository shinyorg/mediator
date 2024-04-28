using Sample.Handlers.MyMessage;

namespace Sample;

// scoped
public class MainViewModel : ViewModel, IEventHandler<MyMessageEvent>
{
    public MainViewModel(BaseServices services, IMediator mediator) : base(services)
    {
        this.TriggerEvent = ReactiveCommand.CreateFromTask(async () =>
        {
            await mediator.Publish(
                new MyMessageEvent("This is my message"),
                fireAndForget: true,
                executeInParallel: true,
                CancellationToken.None
            );
        });
    }


    public ICommand TriggerEvent { get; }
    public async Task Handle(MyMessageEvent @event, CancellationToken cancellationToken)
    {
        // do something - this will trigger on the same instance as the viewmodel
    }
}