using System.Runtime.CompilerServices;

// These types moved to Shiny.Mediator.AppSupport so the Uno Platform and Avalonia integrations can share them.
[assembly: TypeForwardedTo(typeof(Shiny.Mediator.MainThreadAttribute))]
[assembly: TypeForwardedTo(typeof(Shiny.Mediator.Middleware.MainTheadEventMiddleware<>))]
[assembly: TypeForwardedTo(typeof(Shiny.Mediator.Middleware.MainThreadRequestMiddleware<,>))]
[assembly: TypeForwardedTo(typeof(Shiny.Mediator.Middleware.MainThreadCommandMiddleware<>))]
