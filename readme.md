# Shiny Mediator (Preview)

<a href="https://www.nuget.org/packages/Shiny.Mediator" target="_blank">
  <img src="https://buildstats.info/nuget/Shiny.Mediator?includePreReleases=true" />
</a>

A mediator pattern, but for apps.  Apps have pages with lifecycles that don't necessarily participate in the standard 
dependency injection lifecycle.  .NET MAUI generally tends to favor the Messenger pattern.  We hate this pattern for many reasons 
which we won't get into.  That being said, we do offer a messenger subscription in our Mediator for where interfaces
and dependency injection can't reach.

This project is heavily inspired by [MediatR](https://github.com/jbogard/mediatr) with some lesser features that we feel
were aimed more at server scenarios, while also adding some features we feel benefit apps

## Features
* A Mediator for your .NET Apps (MAUI & Blazor are the main targets for us)
* Think of "weak" message subscription without the fuss or mess to cleanup
* Our MAUI & Blazor integrations allow your viewmodels or pages to implement an IEventHandler<TEvent> interface(s) without them having to participate in the dependency injection provider
* We still have a "messagingcenter" type subscribe off IMediator for cases where you can't have your current type implement an interface
* Instead of Assembly Scanning, we have source generators to automatically wireup the necessary registrations for you! (WIP)
* Lightweight, No external dependencies, tiny bit of reflection 
* Help remove service overrun and reduce your constructor fat
* Easy to Unit Test

## Works With
* .NET MAUI - all platforms
* MVVM Frameworks like Prism, ReactiveUI, & .NET MAUI Shell
* Blazor - TBD
* Any other .NET platform - but you'll have to come up with your own "event collector" for the out-of-state stuff 

## Getting Started

Install [Shiny.Mediator](https://www.nuget.org/packages/Shiny.Mediator) from NuGet

First, let's create our request & event handlers

```
using Shiny.Mediator;

public record TestRequest(string AnyArgs, int YouWant) : IRequest;
public record TestEvent(MyObject AnyArgs) : IEvent;

// and for request/response requests - we'll come back to this
public record TestResponseRequest : IRequest<TestResponse> {}
public record TestResponse {}
```

Next - let's wire up a RequestHandler.  You can have ONLY 1 request handler per request type.
This is where you would do the main business logic or data requests.

Let's create our RequestHandler

```csharp
using Shiny.Mediator;

// NOTE: Request handlers are registered as singletons
public class TestRequestHandler : IRequestHandler<TestRequest> 
{
    // you can do all dependency injection here
    public async Task Handle(TestRequest request, CancellationToken ct) 
    {
        // do something async here
    }
}

public class TestResponseRequestHandler : IRequestHandler<TestResponseRequest, TestResponse>
{
    public async Task<TestResponse> Handle(TestResponseRequest request, CancellationToken ct)
    {
        var response = await GetResponseThing(ct);
        return response;
    }
}

public class TestEventHandler : IEventHandler<TestEvent> 
{
    // Dependency injection works here
    public async Task Handle(TestEvent @event, CancellationToken ct)
    {
        // Do something async here
    }
}
```

Now, let's register all of our stuff with our .NET MAUI MauiProgram.cs

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>();
        
        builder.Services.AddShinyMediator();
        builder.Services.AddSingletonAsImplementedInterfaces<TestEventHandler>();
        builder.Services.AddSingletonAsImplementedInterfaces<TestRequestHandler>();
        builder.Services.AddSingltonAsImplementedInterfaces<TestResponseRequestHandler>();
        // OR if you're using our attribute for source generation
    }
}
```

Lastly, any model model/viewmodel/etc participating in dependency injection can now inject the mediator

```
public class MyViewModel(Shiny.Mediator.IMediator mediator)
{
    public async Task Execute() 
    {
        await mediator.Send(new TestRequest()); // this will execute TestRequestHandler
        var response = await mediator.Send(new TestResponseRequest()); // this will execute TestResponseRequestHandler and return a value
        
        // this will publish to any service registered that implement IEventHandler<TestEvent>
        // there are additional args here to allow you to execute values in sequentially or wait for all events to complete
        await mediator.Publish(new TestEvent()); 
    }
}
```

### What about my ViewModels?

For .NET MAUI, your viewmodels have the ability to participate in the event publishing chain without being part of dependency injection

With this setup, you don't need to worry about deallocating any events, unsubscribing from some service, or hooking to anything.

Lastly, if your page/viewmodel is navigated away from (popped), it will no longer participate in the event broadcast

Let's go back to MauiProgram.cs and alter the AddShinyMediator

```csharp
builder.Services.AddShinyMediator<MauiEventCollector>();
```

Now your viewmodel (or page) can simply implement the IEventHandler<T> interface to participate

NOTE: Further example to below - you can implement multiple event handlers (or request handlers)

```csharp
public class MyViewModel : BaseViewModel, 
                           Shiny.Mediator.IEventHandler<TestEvent>,
                           Shiny.Mediator.IEventHandler<TestEvent>
{
    public async Task Handle(TestEvent @event, CancellationToken ct)
    {
    }
    
    public async Task Handle(TestEvent2 @event, CancellationToken ct)
    {
    }
}
```

## Sample
There is a sample in this repo.  You do not need any other part of Shiny, Prism, ReactiveUI, etc - those are included as I write things faster with it.
Focus on the interfaces from the mediator & the mediator calls itself

## Ideas for Workflows
* Use Prism with Modules - want strongly typed navigation parameters, navigations, and have them available to other modules - we're the guy to help!
    * Example TBD
* Using a Shiny Foreground Job - want to push data an event that new data came in to anyone listening?
* Have a Shiny Push Delegate that is executing on the delegate but want to push it to the UI, Mediator has a plan!

## TODO
* Explain Event Collectors 
* Streams - IAsyncEnumerable or IObservable
* Source Generator Registration
  * Need to use a different method or not use extension methods - maybe AddHandlersFromAssemblyName or allow it to be custom named 
* IEventHandler<IEvent> can handle ALL events?
* Request Middleware - Covariance/catch-all