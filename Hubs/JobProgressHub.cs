using Microsoft.AspNetCore.SignalR;

namespace WbtWebJob.Hubs;

public class JobProgressHub : Hub
{
    public async Task SubscribeToJob(string businessId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, businessId);
        await Clients.Caller.SendAsync("Subscribed", new
        {
            BusinessId = businessId,
            Message = "Successfully subscribed to job updates"
        });
    }

    public async Task UnsubscribeFromJob(string businessId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, businessId);
        await Clients.Caller.SendAsync("Unsubscribed", new
        {
            BusinessId = businessId,
            Message = "Successfully unsubscribed from job updates"
        });
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            Message = "Connected to JobProgress Hub"
        });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
