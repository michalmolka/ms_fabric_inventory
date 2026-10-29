using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    public async Task NotifyClients(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}
