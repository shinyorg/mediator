using Microsoft.UI.Xaml;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.UI;

namespace Shiny.Mediator.Infrastructure;


public class UnoEventCollector(IRouteNotifier routeNotifier) : IEventCollector, IServiceInitialize
{
    readonly List<FrameworkElement> events = new();
 
    public void Initialize()
    {
        routeNotifier.RouteChanged += (sender, args) =>
        {
            // var view = ((FrameNavigator)args.Navigator).
            var view = args.Region.View;
            Console.WriteLine("View: " + view?.GetType().FullName);
            Console.WriteLine("DC: " + args.Region.View?.DataContext?.GetType().FullName);
        };
    }
    
    public IReadOnlyList<IEventHandler<TEvent>> GetHandlers<TEvent>() where TEvent : IEvent
    {
        List<FrameworkElement> copy = new();
        // lock (this.events)
        //     copy = this.events.ToList();
        //
        var list = new List<IEventHandler<TEvent>>();
        // foreach (var item in copy)
        // {
        //     if (item.DataContext is IEventHandler<TEvent> vmHandler)
        //         list.Add(vmHandler);
        //
        //     else if (item is IEventHandler<TEvent> viewHandler)
        //         list.Add(viewHandler);
        // }

        return list;
    }
}