using Microsoft.UI.Xaml;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;

namespace Shiny.Mediator.Infrastructure;


public class AlertDialogService(IRouteNotifier routeNotifier) : IAlertDialogService, IServiceInitialize
{
    public void Initialize()
    {
        routeNotifier.RouteChanged += (sender, args) =>
        {
            var view = args.Region.View;
            Console.WriteLine("View: " + view?.GetType().FullName);
            Console.WriteLine("DC: " + args.Region.View?.DataContext?.GetType().FullName);
        };
    }
    
    public void Display(string title, string message)
    {
        // new Navigator(null, null, )
        // Window.Current.CoreWindow
        // try
        // {
        //     await navigator.ShowMessageDialogAsync(
        //         this,
        //         title: title, 
        //         content: message
        //     );
        //     
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine(ex);
        // }
    }
}