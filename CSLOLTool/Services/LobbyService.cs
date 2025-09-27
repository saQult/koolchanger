using CSLOLTool.Models;
using LCUSharp;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace CSLOLTool.Services;

public class LobbyService
{
    private readonly string _url = "https://mrekk.ru/lobbyhub";

    public HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
             .WithUrl(_url)
             .WithServerTimeout(TimeSpan.FromMilliseconds(120000))
             .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)])
             .Build();
    }

    public async Task<LobbyData> ExtractLobbyInfoAsync()
    {
        var api = await LeagueClientApi.ConnectAsync();

        var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, "lol-lobby/v2/lobby");

        using JsonDocument doc = JsonDocument.Parse(json);

        var root = doc.RootElement;

        var localMemberJson = root.GetProperty("localMember");

        var localMember = new LocalMember
        {
            Puuid = localMemberJson.GetProperty("puuid").GetString()!,
        };

        var membersJson = root.GetProperty("members");
        var members = new List<LobbyMember>();

        foreach (var memberJson in membersJson.EnumerateArray())
        {
            members.Add(new LobbyMember
            {
                Puuid = memberJson.GetProperty("puuid").GetString()!,
                IsLeader = memberJson.GetProperty("isLeader").GetBoolean()
            });
        }

        return new LobbyData
        {
            LocalMember = localMember,
            Members = members
        };
    }
}
