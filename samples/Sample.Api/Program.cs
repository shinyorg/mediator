using Microsoft.AspNetCore.Mvc;
using Sample.Api;
using Sample.Api.Handlers;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShinyMediator(x => x.AddMediatorRegistry());
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.MapGeneratedMediatorEndpoints();
app.MapStaticAssets();

// app.MapGet(
//     "/sse",
//     async (
//         [FromServices] IMediator mediator,
//         [FromServices] IHttpContextAccessor context
//     ) =>
//     {
//         context.HttpContext.Response.Headers.ContentType = "text/event-stream";
//         context.HttpContext.Response.Headers.CacheControl = "no-cache";
//         context.HttpContext.Response.Headers.Connection = "keep-alive";
//
//         // while (!context.HttpContext.RequestAborted.IsCancellationRequested)
//         // {
//             var stream = mediator.Request(new TickerStreamRequest(999, 1000));
//             await foreach (var ticker in stream)
//             {
//                 await context.HttpContext.Response.WriteAsync(ticker.Result.ToString());
//                 await context.HttpContext.Response.Body.FlushAsync();
//             }
//         // }
//     }
// );

app.Run();