namespace Sample.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    
    public MainViewModel? ViewModel => DataContext as MainViewModel;
}