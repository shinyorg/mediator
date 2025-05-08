using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sample.Blazor;
using Sample.Blazor.Handlers;
using Shiny.Mediator;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddShinyMediator(x =>
{
    x.UseBlazor();
    x.AddExceptionHandler<TestExceptionHandler>();
});
builder.Services.AddDiscoveredMediatorHandlersFromSample_Blazor();

await builder.Build().RunAsync();