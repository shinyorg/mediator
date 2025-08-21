using Microsoft.Extensions.Hosting;
using Shiny.Mediator;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddShinyMediator(x => x.UseConsole());

var app = builder.Build();
await app.RunMediation(args);

Console.WriteLine("Hello, World!");