using Microsoft.AspNetCore.SignalR;

namespace BigBang1112.Gbx.Server.Hubs;

public class SecureHub : Hub
{
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong");
    }
}
