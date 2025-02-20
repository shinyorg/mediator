namespace Sample.Uno.Presentation;

public sealed partial class SecondPage : Page
{
    public SecondPage()
    {
        this.InitializeComponent();
    }
    
    public SecondViewModel? ViewModel => DataContext as SecondViewModel;
}