# Shiny Mediator

<a href="https://www.nuget.org/packages/Shiny.Mediator" target="_blank">
  <img src="https://img.shields.io/nuget/v/Shiny.Mediator?style=for-the-badge" />
</a>

Mediator is a behavioral design pattern that lets you reduce chaotic dependencies between objects. The pattern restricts direct communications between the objects and forces them to collaborate only via a mediator object.

Shiny Mediator <NugetBadge name="Shiny.Mediator" /> is a mediator pattern implementation, but for built with ALL .NET apps in mind.  We provide a TON of middleware out-of-box to get you up and rolling with
hardly any effort whatsoever.  Checkout our [Getting Started](https://shinylib.net/mediator/getting-started) guide to see how easy it is.  Imagine using 1 line of code to add offline, caching, or validation to your code!

This project is heavily inspired by [MediatR](https://github.com/jbogard/mediatr) with some lesser features that we feel
were aimed more at server scenarios, while also adding some features we feel benefit apps

We are AOT/Trim friendly in all aspects of how you use Mediator.  We use source generators for almost everything (attribute registration, JSON serialization, dependency injection registration, & more)

## Samples & Documentation
- Docs
  - [Main](https://shinylib.net/mediator/)
  - [Getting Started](https://shinylib.net/mediator/getting-started/)
- [Sample](https://github.com/shinyorg/mediator/tree/main/Sample)
- [End-to-End Architectural Layout Sample](https://github.com/shinyorg/mediatorsample)
- [A Cool Offline Capable App with Mediator and Shiny](https://github.com/shinyorg/wonderland)

## Features
- A Mediator for your ALL .NET Apps
- Fully AOT & Trimming friendly
- [Request/Response Handling](https://shinylib.net/mediator/requests)
- [Event Publication](https://shinylib.net/mediator/events)
- [Async Enumerable Stream Requests](https://shinylib.net/mediator/streams)
- Request & event middleware with some great "out of the box" scenarios for your app
- Instead of Assembly Scanning, we have source generators to automatically wireup the necessary registrations for you!
- Think of "weak" message subscriptions without the fuss or mess to cleanup
- Lightweight, No external dependencies, tiny bit of reflection
- Help remove service overrun and reduce your constructor fat
- Easy to Unit Test
- Checkout our [MAUI](https://shinylib.net/mediator/extensions/maui) & [Blazor](https://shinylib.net/mediator/extensions/blazor)
  - Integrations allow your viewmodels or pages to implement an IEventHandler interface(s) without them having to participate in the dependency injection provider
  - Middleware built for apps including caching, offline support, & more
  - We still have a "messagingcenter" type subscribe off IMediator for cases where you can't have your current type implement an interface
- Save the Boilerplate + Receive the Power of Middleware
  - [Dapper Extension](https://shinylib.net/mediator/extensions/dapper) for Easy Query Handling
  - [HTTP Extension](https://shinylib.net/mediator/extensions/http) for Easy API handling - OpenAPI Contract Generation takes it even one step further
  - Map contracts directly to handlers with our [ASP.NET Extension](https://shinylib.net/mediator/extensions/aspnet)
  - Server Sent Events for ASP.NET
- [Epic Out-of-the-Box Middleware](https://shinylib.net/mediator/middleware/)
  - [Offline Data](https://shinylib.net/mediator/middleware/offline)
  - [Caching](https://shinylib.net/mediator/middleware/caching)
  - [Resiliency](https://shinylib.net/mediator/middleware/resilience)
  - [User Exception Handling notifications](https://shinylib.net/mediator/middleware/usererrornotifications)
  - Exception Handling logging
  - [Performance Time Logging](https://shinylib.net/mediator/middleware/performancelogging)
  - [Main Thread Dispatching](https://shinylib.net/mediator/middleware/mainthread)
  - [Replayable Streams](https://shinylib.net/mediator/middleware/replay)
  - [Refresh Timer Streams](https://shinylib.net/mediator/middleware/refresh)
  - [Command Scheduling](https://shinylib.net/mediator/middleware/scheduling)

## Works With
- .NET MAUI - all platforms
- MVVM Frameworks like Prism, ReactiveUI, & .NET MAUI Shell
- Blazor - Work In Progress
- Any other .NET platform - but you'll have to come up with your own "event collector" for the out-of-state stuff

## What Does It Solve

### Problem #1 - Service & Reference Hell

Does this look familiar to you?  Look at all of those injections!  As your app grows, the list will only grow.  I feel sorry for the dude that gets to unit test this bad boy.

```csharp
public class MyViewModel(
    IConnectivity conn,
    IDataService data,
    IAuthService auth,
    IDialogsService dialogs,
    ILogger<MyViewModel> logger
) {
    // ...
    try {
        if (conn.IsConnected) 
        {
            var myData = await data.GetDataRequest();
        }
        else 
        {
            dialogs.Show("No Connection");
            // cache?
        }
    }
    catch (Exception ex) {
        dialogs.Show(ex.Message);
        logger.LogError(ex);
    }
}
```

With a bit of our middleware and some events, you can get here:

```csharp
public partial class MyViewModel(IMediator mediator) : IEventHandler<ConnectivityChangedEvent>, IEventHandler<AuthChangedEvent> {
    // ...
    var myData = await mediator.Request(new GetDataRequest());

    // logging, exception handling, offline caching can all be bundle into one nice clean call without the need for coupling
}

public partial class GetDataRequestHandler : IRequestHandler<GetDataRequest, MyData> {

    [OfflineAvailable] // <= boom done
    public async Task<MyData> Handle(GetDataRequest request, CancellationToken cancellationToken) {
        // ...
    }
}
```

### Problem #2 - Messages EVERYWHERE (+ Leaks)

Do you use the MessagingCenter in Xamarin.Forms?  It's a great tool, but it can lead to some memory leaks if you're not careful.  It also doesn't have
a pipeline, so any errors in any of the responders will crash the entire chain.  It doesn't have a request/response style setup (not that it was meant for it), but
this means you still require other services.

```csharp
public class MyViewModel
{
    public MyViewModel()
    {
        MessagingCenter.Subscribe<SomeEvent1>(this, @event => {
            // do something
        });
        MessagingCenter.Subscribe<SomeEvent2>(this, @event => {
            // do something
        });

        MessagingCenter.Send(new SomeEvent1());
        MessagingCenter.Send(new SomeEvent2());

        // and don't forget to unsubscribe
        MessagingCenter.Unsubscribe<SomeEvent1>(this);
        MessagingCenter.Unsubscribe<SomeEvent2>(this);
    }
}
```

Let's take a look at our mediator in action for this scenarios

```csharp
public class MyViewModel : IEventHandler<SomeEvent1>, IEventHandler<SomeEvent2>
{
    public MyViewModel(IMediator mediator)
    {
        // no need to unsubscribe
        mediator.Publish(new SomeEvent1());
        mediator.Publish(new SomeEvent2());
    }
}
```


### Problem #3 - Strongly Typed Navigation with Strongly Typed Arguments

Our amazing friends over in Prism offer the "best in class" MVVM framework.  We'll them upsell you beyond that, but one
of their amazing features is 'Modules'.  Modules help break up your navigation registration, services, etc.

What they don't solve is providing a strongly typed nature for this stuff (not their job though).  We think we can help
addon to their beautiful solution.

A normal call to a navigation service might look like this:

```csharp
_navigationService.NavigateAsync("MyPage", new NavigationParameters { { "MyArg", "MyValue" } });
```

This is great.  It works, but I don't know the type OR argument requirements of "MyPage" without going to look it up.  In a small project
with a small dev team, this is fine.  In a large project with a large dev team, this can be difficult.

Through our Shiny.Framework library we offer a GlobalNavigationService that can be used to navigate to any page in your app from anywhere, however,
for the nature of this example, we'll pass our navigation service FROM our viewmodel through the mediator request to ensure proper scope.


```csharp
public record MyPageNavigatonRequest(INavigationService navigator, string MyArg) : IRequest;
public class MyPageNavigationHandler : IRequestHandler<MyPageNavigatonRequest>
{
    public async Task Handle(MyPageNavigatonRequest request, CancellationToken cancellationToken)
    {
        await request.navigator.NavigateAsync("MyPage", new NavigationParameters { { "MyArg", request.MyArg } });
    }
}
```

Now, in your viewmodel, you can do this:

```csharp
public class MyViewModel
{
    public MyViewModel(IMediator mediator)
    {
        mediator.Request(new MyPageNavigationCommand(_navigationService, "MyValue"));
    }
}
```

Strongly typed.  No page required page knowledge from the module upfront.  The other dev team of the module can define HOW things work.
