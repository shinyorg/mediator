using System;

namespace Shiny.Mediator.Server;


public interface IServerCommand : ICommand
{
    TimeSpan? TimeToLive { get; set; }
    DateTimeOffset? ScheduledTime { get; set; }
}