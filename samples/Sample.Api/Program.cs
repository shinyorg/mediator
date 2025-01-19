using Sample.Api;
using Sample.Api.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator();
builder.Services.AddDiscoveredMediatorHandlersFromSample_Api();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapMediatorGet<MappedRequest, string>("/mapped").WithOpenApi();
app.UseHttpsRedirection();
app.MapShinyMediatorEndpointHandlers(builder.Services);

app.Run();