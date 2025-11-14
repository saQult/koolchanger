using Microsoft.AspNetCore.SignalR.Client;

namespace CSLOLTool.Services;

public class LobbyService
{
    private readonly string _url = "https://koolchanger.mrekk.ru/lobbyhub";

    public HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
             .WithUrl(_url)
             .WithServerTimeout(TimeSpan.FromMilliseconds(120000))
             .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)])
             .Build();
    }
}
