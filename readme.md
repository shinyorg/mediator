
The purpose of this mediator setup is that it can operate in a mobile platform

- It should fire events on existing viewmodel instances (and cleanup)
- It should fire transient/singleton registrations as well

Thoughts
- If the mediator is scoped, it should be "inlined with viewmodel scope", but it won't be "inlined with all other viewmodels in the nav stack"
- Have a wrapped service provider, for all scoped instances that are resolved, store to a "instance collection" and remove when its service scope is disposed

Pipeline
- Time how long events took, time how long a command took

Streams 
- IAsyncEnumerable or IObservable

Prism Strongly Typed Navigation
- Requires global navigation service