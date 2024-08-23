using Prism.Common;
using Prism.Navigation.Xaml;

namespace Shiny.Mediator.Prism.Infrastructure;


public class GlobalNavigationService(IApplication application) : IGlobalNavigationService
{
    public Task<INavigationResult> GoBackAsync(INavigationParameters parameters)
        => this.Run(nav => nav.GoBackAsync(parameters));

    public Task<INavigationResult> GoBackToAsync(string viewName, INavigationParameters parameters)
        => this.Run(nav => nav.GoBackToAsync(viewName, parameters));

    public Task<INavigationResult> GoBackToRootAsync(INavigationParameters parameters)
        => this.Run(nav => nav.GoBackToRootAsync(parameters));

    public Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameters)
        => this.Run(nav => nav.NavigateAsync(uri, parameters));

    public Task<INavigationResult> SelectTabAsync(string name, Uri uri, INavigationParameters parameters)
        => this.Run(nav => nav.SelectTabAsync(name, parameters));
    
    protected virtual async Task<INavigationResult> Run(Func<INavigationService, Task<INavigationResult>> func)
    {
        var window = application.Windows.OfType<Window>().First();
        var currentPage = MvvmHelpers.GetCurrentPage(window.Page);
        var container = currentPage.GetContainerProvider();

        var navService = container.Resolve<INavigationService>();
        var result = await func(navService);
        return result;
    }
}