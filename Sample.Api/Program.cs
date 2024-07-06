using Sample.Api;

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

app.UseHttpsRedirection();
app.UseShinyMediatorEndpointHandlers(builder.Services);

app.Run();