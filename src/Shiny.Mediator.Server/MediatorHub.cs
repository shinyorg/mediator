using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Shiny.Mediator.Server.Infrastructure;

namespace Shiny.Mediator.Server;


public class MediatorHub(ILogger<MediatorHub> logger, IDataStore store) : Hub
{
    // TODO: return response
    public async Task Push(ServerMessage message)
    {
        // TODO: if scheduled, send to registered inboxes
        
        // if request
            // TODO: handler must be online or return error
            
        // if event
            // this.Clients.Group(eventRequest.EventType).SendAsync(eventRequest);
            // TODO: if 1 or more clusters registered are not online, send to inbox 
    }

    
    public async Task Register(ClusterRegistration register)
    {
        var ipAddress = this.Context.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString();
        
        logger.LogInformation("Registering Client from IP: " + ipAddress);
        // TODO: data store - clear out all event registrations
        // TODO: reregister for events incoming
        // TODO: add event types to group for perf push
            // TODO: delete any event inboxes that aren't in this latest batch (and registrations)
            // TODO: don't just clear all inboxes and recreate as messages will be lost
        foreach (var e in register.SubscribedEventTypes)
        {
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, e);
        }
        
        // TODO: own commands coming in
            // TODO: warn if already owned or error?
            // TODO: logical scale outs - multiple apps, but only one can receive
                
        // TODO: send any events that remain in inbox that have not expired
    }    
}
