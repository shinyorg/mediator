using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny.Mediator;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    // turn on error trapping for ALL message handlers and cultures - this is turned off to show how to specifically pick handlers which is a better design model anyhow
    // { "Mediator:UserErrorNotifications:*:*:Message", "UserErrorNotification is emitting this message" }
    { "Mediator:UserErrorNotifications:Sample.Blazor.Handlers.SafeErrorCommand:*:Message", "UserErrorNotification is emitting this message" },
    { "Mediator:UserErrorNotifications:Sample.Blazor.Handlers.SafeErrorCommand:fr-CA:Message", "UserErrorNotification is emitting this message for French Users" },
}!);

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddShinyMediator(x =>
{
    x.Services.AddMediatorRegistry();
    x.UseBlazor();
});

await builder.Build().RunAsync();