using Microsoft.AspNetCore.Mvc;
using Sample.Api;
using Sample.Api.Handlers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x => x.AddMediatorRegistry());
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapGeneratedMediatorEndpoints();
app.UseDefaultFiles().UseStaticFiles();

app.MapGet(
    "/sse",
    (
        [FromServices] IMediator mediator,
        [FromServices] IHttpContextAccessor context,
        [AsParameters] TickerStreamRequest request
    ) => mediator.RequestServerSentEvents(request, context)
).ExcludeFromDescription();

app.Run();