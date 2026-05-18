using Prism.Common;
using Prism.Navigation.Xaml;

namespace Shiny.Mediator.Prism.Infrastructure;


/// <summary>
/// Default <see cref="IGlobalNavigationService"/> implementation that locates the current Prism
/// page on the first <see cref="Window"/> and forwards each <see cref="INavigationService"/> call
/// to the page-scoped navigation service resolved from its container.
/// </summary>
public class GlobalNavigationService(IApplication application) : IGlobalNavigationService
{
    /// <summary>Forwards <c>GoBackAsync</c> to the current page's resolved <see cref="INavigationService"/>.</summary>
    public Task<INavigationResult> GoBackAsync(INavigationParameters parameters)
        => this.Run(nav => nav.GoBackAsync(parameters));

    /// <summary>Forwards <c>GoBackToAsync</c> to the current page's resolved <see cref="INavigationService"/>.</summary>
    public Task<INavigationResult> GoBackToAsync(string viewName, INavigationParameters parameters)
        => this.Run(nav => nav.GoBackToAsync(viewName, parameters));

    /// <summary>Forwards <c>GoBackToRootAsync</c> to the current page's resolved <see cref="INavigationService"/>.</summary>
    public Task<INavigationResult> GoBackToRootAsync(INavigationParameters parameters)
        => this.Run(nav => nav.GoBackToRootAsync(parameters));

    /// <summary>Forwards <c>NavigateAsync</c> to the current page's resolved <see cref="INavigationService"/>.</summary>
    public Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameters)
        => this.Run(nav => nav.NavigateAsync(uri, parameters));

    /// <summary>Forwards <c>SelectTabAsync</c> to the current page's resolved <see cref="INavigationService"/>.</summary>
    public Task<INavigationResult> SelectTabAsync(string name, Uri uri, INavigationParameters parameters)
        => this.Run(nav => nav.SelectTabAsync(name, parameters));

    /// <summary>
    /// Resolves the current page's container-scoped <see cref="INavigationService"/> and invokes
    /// <paramref name="func"/> against it. Override to customise window/page lookup.
    /// </summary>
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