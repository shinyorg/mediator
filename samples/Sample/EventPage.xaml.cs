namespace Sample;

public partial class EventPage : ContentPage
{
    public EventPage(EventViewModel viewModel)
    {
        this.InitializeComponent();
        this.BindingContext = viewModel;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((EventViewModel)this.BindingContext).OnAppearing();
    }
}