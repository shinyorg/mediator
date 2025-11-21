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
        [AsParameters] TickerStreamRequest request
    ) => mediator.Request(request)
);

app.MapGet(
    "/pub",
    async (
        [FromServices] IMediator mediator
    ) => 
    {
        await mediator.Publish(new MyEvent());
        return Results.Ok();
    }
);

app.MapGet(
    "/sub",
    ([FromServices] IMediator mediator) => TypedResults.ServerSentEvents(mediator.EventStream<MyEvent>())
);

app.Run();

public record MyEvent : IEvent;