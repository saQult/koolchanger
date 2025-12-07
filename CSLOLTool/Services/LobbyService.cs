using Microsoft.AspNetCore.SignalR.Client;

namespace CSLOLTool.Services;

public class LobbyService
{
    public HubConnection CreateConnection(string url = "http://188.68.220.248:5000/lobbyhub")
    {
        return new HubConnectionBuilder()
             .WithUrl(url)
             .WithServerTimeout(TimeSpan.FromMilliseconds(120000))
             .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)])
             .Build();
    }
}