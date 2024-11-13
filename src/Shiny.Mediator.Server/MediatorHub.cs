using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;

namespace Shiny.Mediator.Server;


public class MediatorHub : Hub
{
    // TODO: return response
    public async Task Request()
    {
        // TODO: handler must be online or return error
        
    }


    public async Task Publish(EventRequest eventRequest)
    {
        // TODO: if scheduled, send to registered inboxes
        // TODO: if registration not online, send to inbox 
        // this.Clients.Group(eventRequest.EventType).SendAsync(eventRequest);
    }
    
    
    public async Task Register(AppRegistration register)
    {
        // TODO: data store - clear out all event registrations
        // TODO: reregister for events incoming
        // TODO: add event types to group for perf push
        foreach (var e in register.EventTypes)
        {
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, e);
        }
        
        // TODO: own commands coming in
            // TODO: warn if already owned or error?
            // TODO: logical scale outs - multiple apps, but only one can receive
                
        // TODO: send any events that remain in inbox that have not expired
    }    
}

public record AppRegistration(
    string AppIdentifier,
    string[] OwnedCommandTypes,
    string[] EventTypes
);

public record EventRequest(
    string EventType,
    JsonObject Payload,
    DateTimeOffset? ScheduledTime,
    DateTimeOffset? ExpiresAt
);