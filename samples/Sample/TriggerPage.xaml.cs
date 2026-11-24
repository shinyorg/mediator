namespace Sample;


public partial class TriggerPage : ContentPage
{
    public TriggerPage(TriggerViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }
}