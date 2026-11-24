namespace Sample;

public partial class BlazorPage : ContentPage
{
    public BlazorPage(BlazorViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}