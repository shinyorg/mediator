# Shiny Mediator (Preview)

<a href="https://www.nuget.org/packages/Shiny.Mediator" target="_blank">
  <img src="https://buildstats.info/nuget/Shiny.Mediator?includePreReleases=true" />
</a>

Mediator is a behavioral design pattern that lets you reduce chaotic dependencies between objects. The pattern restricts direct communications between the objects and forces them to collaborate only via a mediator object.

Shiny Mediator is a mediator pattern implementation, but for apps.  Apps have pages with lifecycles that don't necessarily participate in the standard
dependency injection lifecycle.  .NET MAUI generally tends to favor the Messenger pattern.  We hate this pattern for many reasons
which we won't get into.  That being said, we do offer a messenger subscription in our Mediator for where interfaces
and dependency injection can't reach.

This project is heavily inspired by [MediatR](https://github.com/jbogard/mediatr) with some lesser features that we feel
were aimed more at server scenarios, while also adding some features we feel benefit apps

## Links
- Docs
  - [Main](https://shinylib.net/client/mediator/)
  - [Quick Start](https://shinylib.net/client/mediator/quick-start/)
  - [Middleware](http://localhost:4321/client/mediator/middleware)
  - [Advanced](http://localhost:4321/client/mediator/advanced/)
- [Sample](https://github.com/shinyorg/mediator/tree/main/Sample)

## Features
- A Mediator for your .NET Apps (MAUI & Blazor are the main targets for us)
- Request/Response "Command" Handling
- Event Publication
- Request & event middleware with some great "out of the box" scenarios for your app
- Think of "weak" message subscriptions without the fuss or mess to cleanup
- Our MAUI & Blazor integrations allow your viewmodels or pages to implement an IEventHandler interface(s) without them having to participate in the dependency injection provider
- We still have a "messagingcenter" type subscribe off IMediator for cases where you can't have your current type implement an interface
- Instead of Assembly Scanning, we have source generators to automatically wireup the necessary registrations for you! (WIP)
- Lightweight, No external dependencies, tiny bit of reflection
- Help remove service overrun and reduce your constructor fat
- Easy to Unit Test

## Works With
- .NET MAUI - all platforms
- MVVM Frameworks like Prism, ReactiveUI, & .NET MAUI Shell
- Blazor - Work In Progress
- Any other .NET platform - but you'll have to come up with your own "event collector" for the out-of-state stuff