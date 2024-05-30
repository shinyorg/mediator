# Shiny Mediator

A mediator pattern, but for apps.  Apps have pages with lifecycles that don't necessarily participate in the standard 
dependency injection lifecycle.  .NET MAUI generally tend to favor the Messenger pattern.  We hate this pattern for many reasons 
which we won't get into.  That being said, we do offer a messenger subscription in our Mediator for where interfaces
and dependency injection can't reach.

Our event publishing comes with a couple of nice flavors in that you can
* Fire & Forget
* Run in parallel or sequentially

## Works with
* .NET MAUI - all platforms
* MVVM Frameworks like Prism, ReactiveUI, & .NET MAUI Shell
* Blazor - TBD
* Any other .NET platform - but you'll have to come up with your own "event collector" for the out-of-state stuff

## Sample
TODO

You do not need any other part of Shiny, Prism, ReactiveUI, etc - those are included as I write things faster with it.
Focus on the interfaces from the mediator & the mediator calls itself

## Ideas for workflows

* Use Prism with Modules - want strongly typed navigation parameters, navigations, and have them available to other modules - we're the guy to help!
    * Example TBD
* Using a Shiny Foreground Job - want to push data an event that new data came in to anyone listening?

## Event Collectors
TODO

* Blazor
* .NET MAUI

## TODO
* Pipelines
  * Error handlers - requests and events?
  * Pre/Post Execution - Time how long events took, time how long a command took

* Streams - IAsyncEnumerable or IObservable
