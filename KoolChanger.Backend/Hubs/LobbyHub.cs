using System.Text.Json.Serialization;
using KoolChanger.Backend.Models;
using KoolChanger.Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace KoolChanger.Backend.Hubs;

public class LobbyHub : Hub
{
    private readonly ILobbyService _lobbyService;
    private readonly ILogger<LobbyHub> _logger;

    public LobbyHub(ILobbyService lobbyService, ILogger<LobbyHub> logger)
    {
        _lobbyService = lobbyService;
        _logger = logger;
    }

    public async Task CreateLobby(string lobbyId, string puuid)
    {
        if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(puuid))
        {
            await Clients.Caller.SendAsync("InvalidLobbyRequest");
            return;
        }

        var connectionId = Context.ConnectionId;

        if (!_lobbyService.TryCreateLobby(lobbyId, connectionId, puuid, out var member))
        {
            await Clients.Caller.SendAsync("LobbyExists", lobbyId);
            return;
        }

        _logger.LogInformation("Lobby {LobbyId} created by {ConnectionId}", lobbyId, connectionId);

        await Groups.AddToGroupAsync(connectionId, lobbyId);
        await Clients.Caller.SendAsync("LobbyCreated", lobbyId, member);
    }

    public async Task<bool> JoinLobby(string lobbyId, string puuid)
    {
        if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(puuid))
        {
            await Clients.Caller.SendAsync("InvalidLobbyRequest");
            return false;
        }

        var connectionId = Context.ConnectionId;

        if (!_lobbyService.TryJoinLobby(lobbyId, connectionId, puuid, out var member))
        {
            await Clients.Caller.SendAsync("LobbyNotFound", lobbyId);
            return false;
        }

        _logger.LogInformation("Connection {ConnectionId} joined lobby {LobbyId}", connectionId, lobbyId);

        await Groups.AddToGroupAsync(connectionId, lobbyId);
        await Clients.Group(lobbyId).SendAsync("MemberJoined", member);

        return true;
    }

    public async Task SendMessage(string lobbyId, string message)
    {
        if (string.IsNullOrWhiteSpace(lobbyId) || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var sender = _lobbyService.GetMember(lobbyId, Context.ConnectionId);
        if (sender != null)
        {
            await Clients.Group(lobbyId).SendAsync("ReceiveMessage", lobbyId, sender.Puuid, message);
        }
    }

    public async Task LeaveLobby()
    {
        if (_lobbyService.TryLeaveLobby(Context.ConnectionId, out var member, out var lobbyId, out var lobbyRemoved) &&
            member != null &&
            lobbyId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobbyId);
            await Clients.Group(lobbyId).SendAsync("MemberLeft", member.Puuid);

            if (lobbyRemoved)
            {
                _logger.LogInformation("Lobby {LobbyId} closed", lobbyId);
                await Clients.All.SendAsync("LobbyClosed", lobbyId);
            }
        }
    }

    public Task<List<LobbyMember>> GetLobbyMembers(string lobbyId)
    {
        var members = _lobbyService
            .GetLobbyMembers(lobbyId)
            .ToList();

        return Task.FromResult(members);
    }

    public Task<bool> IsLobbyAlive(string lobbyId)
    {
        return Task.FromResult(_lobbyService.IsLobbyAlive(lobbyId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveLobby();
        _logger.LogInformation($"Client disconnected: ConnID: {Context.ConnectionId}, UI: {Context.UserIdentifier} {(Context.User)}");
        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: ConnID: {Context.ConnectionId}, UI: {Context.UserIdentifier} {(Context.User)}");
        await base.OnConnectedAsync();
    }
}