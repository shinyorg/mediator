using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Shiny.Mediator;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
{
    { "Mediator:UserErrorNotifications:Sample.Blazor.Handlers.SafeErrorCommand:*:Message", "UserErrorNotification is emitting this message" },
    { "Mediator:UserErrorNotifications:Sample.Blazor.Handlers.SafeErrorCommand:fr-CA:Message", "UserErrorNotification is emitting this message for French Users" },
}!);


builder.Services.AddShinyMediator(x =>
{
    x.UseBlazor();
});
builder.Services.AddDiscoveredMediatorHandlersFromSample_Blazor();

await builder.Build().RunAsync();