using Sample.Handlers.MyMessage;
using Shiny.Mediator;
using Shiny.Mediator.Contracts;
using ICommand = System.Windows.Input.ICommand;

namespace Sample;

// scoped
public class MainViewModel : ViewModel, IEventHandler<MyMessage>
{
    public MainViewModel(BaseServices services, IMediator mediator) : base(services)
    {
        this.TriggerEvent = ReactiveCommand.CreateFromTask(async () =>
        {
            await mediator.Publish(
                new MyMessage("This is my message"),
                fireAndForget: true,
                executeInParallel: true,
                CancellationToken.None
            );
        });
    }


    public ICommand TriggerEvent { get; }
    public async Task Handle(MyMessage @event, CancellationToken cancellationToken)
    {
        // do something - this will trigger on the same instance as the viewmodel
    }
}