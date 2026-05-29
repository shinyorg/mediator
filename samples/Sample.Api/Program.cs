using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sample.Api;
using Sample.Api.Handlers;
using Scalar.AspNetCore;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x =>
{
    x.AddMediatorRegistry();
    x.AddTickerQCommandScheduling();
});
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// TickerQ backs ICommandScheduler with a durable, EF Core-persisted store (Sqlite here).
builder.Services.AddTickerQ(options =>
    options.AddOperationalStore<TimeTickerEntity, CronTickerEntity>(ef =>
        ef.UseTickerQDbContext<TickerQDbContext>(db => db.UseSqlite("Data Source=tickerq.db"))
    )
);

var app = builder.Build();

// create the TickerQ schema for the demo (use EF migrations in a real app)
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetService<IDbContextFactory<TickerQDbContext>>();
    if (factory != null)
    {
        await using var ctx = await factory.CreateDbContextAsync();
        await ctx.Database.EnsureCreatedAsync();
    }
    else
    {
        var ctx = scope.ServiceProvider.GetRequiredService<TickerQDbContext>();
        await ctx.Database.EnsureCreatedAsync();
    }
}
app.UseTickerQ();

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

app.MapPost(
    "/schedule-greeting",
    async (
        [FromServices] IMediator mediator,
        string message,
        int seconds
    ) =>
    {
        await mediator.Send(new ScheduledGreeting
        {
            Message = message,
            DueAt = DateTimeOffset.UtcNow.AddSeconds(seconds)
        });
        return Results.Accepted();
    }
);

app.Run();

public record MyEvent : IEvent;