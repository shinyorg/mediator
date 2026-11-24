namespace Sample;


public class AppShell : Shell
{
    public AppShell()
    {
        Routing.RegisterRoute(nameof(AnotherPage), typeof(AnotherPage));

        this.Items.Add(new TabBar
        {
            Items =
            {
                Tab<TriggerPage>("Trigger"),
                Tab<EventPage>("Events"),
                Tab<BlazorPage>("Blazor")
            }
        });
    }


    // pages (and their view models) are resolved from DI - see MauiProgram
    static ShellContent Tab<TPage>(string title) where TPage : Page => new()
    {
        Title = title,
        Route = typeof(TPage).Name,
        ContentTemplate = new DataTemplate(typeof(TPage))
    };
}
