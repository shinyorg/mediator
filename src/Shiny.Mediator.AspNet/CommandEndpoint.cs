using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shiny.Mediator;

public class WebRequest : IRequest<IResult>;


// all endpoints get mapped to this, url with request type is kept in lookup?
// TODO: I want some middleware to execute here that allows me to transform request?
public class WebRequestHandler(IHttpContextAccessor httpContext) : IRequestHandler<WebRequest, IResult>
{
    // TODO: all endpoints should be scoped so DI can be sent
        // TODO: IHttpContext should be passed to each execution
    public Task<IResult> Handle(WebRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        return new RequestEndpoint<WebRequest>().WebHandle(request, context, httpContext, cancellationToken);
    }
}

public class RequestEndpoint<TRequest> where TRequest : IRequest<IResult>
{
    // TODO: route is really decided by url, contract in/out
    HttpMethod httpMethod = HttpMethod.Get;
    string routePattern;
    readonly List<Action<RouteHandlerBuilder>> configurators = new();
    
    
    WebApplication app;
    
    public void Map(WebApplication app)
    {
        // TODO: group?
        // TODO: get/post/put/delete - with url pattern
        
        
        // pass the builder up?
        // builder.CacheOutput(x => x.Cache())
        // builder.Produces()
    }

    
    protected void Get(string pattern, Action<RouteHandlerBuilder>? config = null)
    {
        var builder = app.MapMediatorGet<TRequest, IResult>(pattern);
        // builder.RequireRateLimiting(new RateLimitPartition<string>("", x =>
        // {
        //     x.
        // }))
        config?.Invoke(builder);
    }


    protected void Post(string pattern, Action<RouteHandlerBuilder>? config = null)
    {
        var builder = app.MapMediatorPost<TRequest, IResult>(pattern);
        config?.Invoke(builder);
    }


    protected void Put(string pattern, Action<RouteHandlerBuilder>? config = null)
    {
        var builder = app.MapMediatorPut<TRequest, IResult>(pattern);
        config?.Invoke(builder);
    }


    protected void Delete(string pattern, Action<RouteHandlerBuilder>? config = null)
    {
        var builder = app.MapMediatorDelete<TRequest, IResult>(pattern);
        config?.Invoke(builder);
    }


    protected void AllowAnonymous()
    {
        
    }


    protected void RequiresAuthorization(params string[] policies)
    {
            // Action<AuthorizationPolicyBuilder>
    }
    
    /*
    public static RouteHandlerBuilder MapMediatorGet<TCommand>(this IEndpointRouteBuilder builder, string pattern)
           where TCommand : ICommand
       => builder.MapGet(
           pattern, 
           async (
               [FromServices] IMediator mediator,
               [AsParameters] TCommand command,
               CancellationToken cancellationToken
           ) =>
           {
               await mediator
                   .Send(command, cancellationToken)
                   .ConfigureAwait(false);

               return Results.Ok();
           }
       );
     */
    
    // TODO: server sent events with streams?
    // public override void Configure()
    // {
    //     Post("/order/create");
    //     Throttle(
    //         hitLimit: 120,
    //         durationSeconds: 60,
    //         headerName: "X-Client-Id" // this is optional
    //     );
    // Options(x => x.CacheOutput(p => p.Expire(TimeSpan.FromSeconds(60))));
    // }

    public async Task<IResult> WebHandle(
        TRequest request, 
        IMediatorContext context, 
        IHttpContextAccessor httpContext, 
        CancellationToken cancellationToken
    )
    {
        // Results<Test>;
        // httpContext.HttpContext
        return Results.Ok("Hello");
    }
}