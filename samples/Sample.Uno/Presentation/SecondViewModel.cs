using Sample.Handlers;
using Shiny.Mediator;

namespace Sample.Uno.Presentation;


public partial class SecondViewModel(
    IMediator mediator, 
    INavigator navigator
) : ObservableObject, IEventHandler<AppEvent>
{
    [RelayCommand]
    Task GoBack() => navigator.GoBack(this);


    [RelayCommand]
    Task PublishEvent() => mediator.Publish(new AppEvent("Hello from SecondPage"));

    
    public async Task Handle(AppEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine(@event.Message);
    }
}