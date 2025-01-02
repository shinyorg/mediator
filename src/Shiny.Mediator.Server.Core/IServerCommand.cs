using System;

namespace Shiny.Mediator.Server;


public interface IServerCommand : IRequest
{
    TimeSpan? TimeToLive { get; set; }
    DateTimeOffset? ScheduledTime { get; set; }
}