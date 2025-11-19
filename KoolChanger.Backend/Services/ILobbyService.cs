using KoolChanger.Backend.Models;

namespace KoolChanger.Backend.Services;

public interface ILobbyService
{
    /// <summary>
    /// Try create lobby
    /// </summary>
    /// <param name="lobbyId"></param>
    /// <param name="connectionId"></param>
    /// <param name="puuid"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    bool TryCreateLobby(string lobbyId, string connectionId, string puuid, out LobbyMember member);

    bool TryJoinLobby(string lobbyId, string connectionId, string puuid, out LobbyMember member);

    bool TryLeaveLobby(string connectionId, out LobbyMember? member, out string? lobbyId, out bool lobbyRemoved);

    IReadOnlyCollection<LobbyMember> GetLobbyMembers(string lobbyId);

    bool IsLobbyAlive(string lobbyId);

    LobbyMember? GetMember(string lobbyId, string connectionId);
}