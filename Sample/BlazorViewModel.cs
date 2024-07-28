using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sample;


public partial class BlazorViewModel(INavigationService navigator) : ObservableObject
{
    [RelayCommand] Task Navigate() => navigator.NavigateAsync(nameof(AnotherPage));
}