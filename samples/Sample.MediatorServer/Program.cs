using Shiny.Mediator.Server;

var builder = WebApplication.CreateBuilder(args);
builder.AddMediatorServer();

var app = builder.Build();
app.UseMediatorServer();

app.Run();