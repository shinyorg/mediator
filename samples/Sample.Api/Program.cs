using Sample.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x => x.AddGeneratedEndpoints());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapGeneratedMediatorEndpoints();

app.Run();