namespace Sample;


public partial class AnotherPage : ContentPage
{
    public AnotherPage(AnotherViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}