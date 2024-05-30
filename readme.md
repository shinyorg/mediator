

The purpose of this mediator setup is that it can operate in a mobile platform

- It should fire events on existing viewmodel instances without the potential for leaking memory
- Event handlers can be "pulled" from other various contexts like a MAUI navigation stack or TBD Blazor

Pipeline
- Time how long events took, time how long a command took

Streams 
- IAsyncEnumerable or IObservable

Prism Strongly Typed Navigation
- Requires global navigation service

Event Collectors
- MAUI - Shell
- Prism Navigator
- Blazor?