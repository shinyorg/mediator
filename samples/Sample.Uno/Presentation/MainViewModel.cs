using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Extensions.Navigation;

namespace Sample.Uno.Presentation;

public partial class MainViewModel : ObservableObject
{
    private INavigator _navigator;

    [ObservableProperty] private string? name;

    public MainViewModel(
        IOptions<AppConfig> appInfo,
        INavigator navigator)
    {
        _navigator = navigator;
        Title = "Main";
        Title += $" - {appInfo?.Value?.Environment}";
        GoToSecond = new AsyncRelayCommand(GoToSecondView);
    }

    public string? Title { get; }

    public ICommand GoToSecond { get; }

    private async Task GoToSecondView()
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new Entity(Name!));
    }
}