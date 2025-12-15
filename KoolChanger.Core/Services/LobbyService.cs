using Microsoft.AspNetCore.SignalR.Client;

namespace KoolChanger.Core.Services;

public class LobbyService
{
    public HubConnection CreateConnection(string url)
    {
        return new HubConnectionBuilder()
             .WithUrl(url)
             .WithServerTimeout(TimeSpan.FromMilliseconds(120000))
             .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)])
             .Build();
    }
}