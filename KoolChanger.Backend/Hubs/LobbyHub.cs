using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using KoolChanger.Backend.Models;

namespace KoolChanger.Backend.Hubs;

public class LobbyHub : Hub
{
    private static ConcurrentDictionary<string, Lobby> _lobbies = new();
    private static ConcurrentDictionary<string, string> _memberLobbyMap = new();

    public async Task CreateLobby(string lobbyId, string puuid)
    {
        if (_lobbies.ContainsKey(lobbyId))
        {
            await Clients.Caller.SendAsync("LobbyExists", lobbyId);
            return;
        }

        var member = new LobbyMember
        {
            ConnectionId = Context.ConnectionId,
            Puuid = puuid
        };

        var lobby = new Lobby
        {
            LobbyId = lobbyId,
            Members = new List<LobbyMember> { member }
        };

        _lobbies[lobbyId] = lobby;
        _memberLobbyMap[Context.ConnectionId] = lobbyId;

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        await Clients.Caller.SendAsync("LobbyCreated", lobbyId, member);
    }

    public async Task<bool> JoinLobby(string lobbyId, string puuid)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            await Clients.Caller.SendAsync("LobbyNotFound", lobbyId);
            return false;
        }

        var member = new LobbyMember
        {
            ConnectionId = Context.ConnectionId,
            Puuid = puuid
        };

        lobby.Members.Add(member);
        _memberLobbyMap[Context.ConnectionId] = lobbyId;

        await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
        await Clients.Group(lobbyId).SendAsync("MemberJoined", member);

        return true;
    }

    public async Task SendMessage(string lobbyId, string message)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            var sender = lobby.Members.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (sender != null)
            {
                await Clients.Group(lobbyId).SendAsync("ReceiveMessage", lobbyId, sender.Puuid, message);
            }
        }
    }

    public async Task LeaveLobby()
    {
        if (_memberLobbyMap.TryRemove(Context.ConnectionId, out var lobbyId) &&
            _lobbies.TryGetValue(lobbyId, out var lobby))
        {
            var member = lobby.Members.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (member != null)
            {
                lobby.Members.Remove(member);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
                await Clients.Group(lobbyId).SendAsync("MemberLeft", member.Puuid);
            }

            if (lobby.Members.Count == 0)
            {
                _lobbies.TryRemove(lobbyId, out _);
                await Clients.All.SendAsync("LobbyClosed", lobbyId);
            }
        }
    }

    public async Task<List<LobbyMember>> GetLobbyMembers(string lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return lobby.Members;
        }

        return new List<LobbyMember>();
    }

    public Task<bool> IsLobbyAlive(string lobbyId)
    {
        return Task.FromResult(_lobbies.ContainsKey(lobbyId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveLobby();
        await base.OnDisconnectedAsync(exception);
    }
}