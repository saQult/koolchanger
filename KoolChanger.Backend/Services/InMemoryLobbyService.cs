using System.Collections.Concurrent;
using KoolChanger.Backend.Models;

namespace KoolChanger.Backend.Services;

public class InMemoryLobbyService : ILobbyService
{
    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, string> _memberLobbyMap = new();

    public bool TryCreateLobby(string lobbyId, string connectionId, string puuid, out LobbyMember member)
    {
        member = new LobbyMember
        {
            ConnectionId = connectionId,
            Puuid = puuid
        };

        var lobby = new Lobby
        {
            LobbyId = lobbyId,
            Members = new List<LobbyMember> { member }
        };

        if (!_lobbies.TryAdd(lobbyId, lobby))
        {
            member = null!;
            return false;
        }

        _memberLobbyMap[connectionId] = lobbyId;
        return true;
    }

    public bool TryJoinLobby(string lobbyId, string connectionId, string puuid, out LobbyMember member)
    {
        member = new LobbyMember
        {
            ConnectionId = connectionId,
            Puuid = puuid
        };

        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            member = null!;
            return false;
        }

        lock (lobby.Members)
        {
            lobby.Members.Add(member);
        }

        _memberLobbyMap[connectionId] = lobbyId;
        return true;
    }

    public bool TryLeaveLobby(string connectionId, out LobbyMember? member, out string? lobbyId, out bool lobbyRemoved)
    {
        member = null;
        lobbyId = null;
        lobbyRemoved = false;

        if (!_memberLobbyMap.TryRemove(connectionId, out var existingLobbyId))
        {
            return false;
        }

        lobbyId = existingLobbyId;

        if (!_lobbies.TryGetValue(existingLobbyId, out var lobby))
        {
            return false;
        }

        lock (lobby.Members)
        {
            member = lobby.Members.FirstOrDefault(x => x.ConnectionId == connectionId);
            if (member != null)
            {
                lobby.Members.Remove(member);
            }

            if (lobby.Members.Count == 0)
            {
                _lobbies.TryRemove(existingLobbyId, out _);
                lobbyRemoved = true;
            }
        }

        return member != null;
    }

    public IReadOnlyCollection<LobbyMember> GetLobbyMembers(string lobbyId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            lock (lobby.Members)
            {
                return lobby.Members.ToList();
            }
        }

        return Array.Empty<LobbyMember>();
    }

    public bool IsLobbyAlive(string lobbyId)
    {
        return _lobbies.ContainsKey(lobbyId);
    }

    public LobbyMember? GetMember(string lobbyId, string connectionId)
    {
        if (_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            lock (lobby.Members)
            {
                return lobby.Members.FirstOrDefault(x => x.ConnectionId == connectionId);
            }
        }

        return null;
    }
}

