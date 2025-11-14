using CSLOLTool.Models;
using LCUSharp.Websocket;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CSLOLTool.Services;

public class PartyService
{
    // Services
    private readonly LCUService _lcuService = new();
    private readonly LobbyService _lobbyService = new();
    private HubConnection? _hubConnection;
    private LobbyData? _currentLobby;

    // Events
    public event Action? Enabled;
    public event Action? Disabled;
    public event Action<string>? OnError;
    public event Action<string>? OnLog;
    public event Action<Skin>? SkinRecieved;
    public event Action<Skin>? SkinSended;
    public event Action<LobbyData>? LobbyJoined;
    public event Action? LobbyLeaved;

    // State
    private bool _isSkinSelected = false;
    private List<Champion> _champions;
    private Skin _selectedSkin = new();
    public Dictionary<Champion, Skin> BackupedSkins = [];
    public Dictionary<Champion, Skin> SelectedSkins { get; set; }
    public string SelectedChampionId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    public PartyService(List<Champion> champions, Dictionary<Champion, Skin> selectedSkins)
    {
        _champions = champions;
        SelectedSkins = selectedSkins;
    }

    public async Task<bool> EnableAsync(Dictionary<Champion, Skin> skins)
    {
        if (!Process.GetProcessesByName("LeagueClient").Any())
        {
            OnError?.Invoke("Please launch League of Legends before enabling party mode");
            return false;
        }
        BackupedSkins = skins;
        await _lcuService.ConnectAsync();

        if (_lcuService.Api != null)
        {
            var gameflowPhase = await _lcuService.Api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, "/lol-gameflow/v1/gameflow-phase");
            if (gameflowPhase != "\"None\"")
            {
                try
                {
                    _currentLobby = await _lcuService.ExtractLeagueLobbyAsync();
                    await ConnectToLobbyAsync(_currentLobby);
                }
                catch { return false; }
            }
        }

        _lcuService.GameFlowChanged += OnGameFlowChanged;
        _lcuService.ChampionSelected += OnChampionSelected;
        _lcuService.SubscrbeLobbyEvent();
        _lcuService.SubscrbeLChampionSelect();
        IsEnabled = true;
        return true;
    }
    public async Task DisableAsync()
    {
        if (_hubConnection != null)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
                await _hubConnection.InvokeAsync("LeaveLobby");
            await _hubConnection!.DisposeAsync();
            LobbyLeaved?.Invoke();
        }
        if (_lcuService.Api != null)
            _lcuService.Api!.Disconnect();

        Disabled?.Invoke();
        IsEnabled = false;
    }
    private async Task ConnectToLobbyAsync(LobbyData lobby)
    {
        try
        {
            _hubConnection = _lobbyService.CreateConnection();
            await _hubConnection.StartAsync();
            await JoinOrCreateLobbyAsync(lobby);

            RegisterLobbyHandlers();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to connect to looby: {ex.Message}");
            try
            {
                await _hubConnection!.StopAsync();
            }
            catch { }
        }

    }
    private void RegisterLobbyHandlers()
    {
        if (_hubConnection == null)
            return;

        _hubConnection.On("MemberJoined", (Func<LobbyMember, Task>)(async member =>
        {
            OnLog?.Invoke($"Member {member.Puuid} joined lobby");

            var members = await _hubConnection.InvokeAsync<List<LobbyMember>>("GetLobbyMembers", _currentLobby!.LobbyId);
            OnLog?.Invoke($"Members count: {members.Count}");

            if (_isSkinSelected == false) return;
            try
            {
                await SendSkinDataToPartyAsync(_selectedSkin);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Error sending skins: " + ex.Message);
            }
        }));

        _hubConnection.On<string, string, string>("ReceiveMessage", (lobbyId, puuid, msg) =>
        {
            if (puuid == _currentLobby!.LocalMember.Puuid)
                return;

            var skin = JsonConvert.DeserializeObject<Skin>(msg);
            if (skin == null)
                return;

            SkinRecieved?.Invoke(skin);
        });

        _hubConnection.Closed += async (error) =>
        {
            await Task.Delay(1000);
            try
            {
                await _hubConnection.StartAsync();
            }
            catch { }
        };
    }
    private async Task JoinOrCreateLobbyAsync(LobbyData lobby)
    {
        bool lobbyFound = false;
        foreach (var member in lobby.Members)
        {
            OnLog?.Invoke($"Trying to connect to lobby {member.Puuid}");

            var result = await _hubConnection!.InvokeAsync<bool>("JoinLobby", member.Puuid, lobby.LocalMember.Puuid);
            if (result)
            {
                _currentLobby!.LobbyId = member.Puuid;

                var members = await _hubConnection!.InvokeAsync<List<LobbyMember>>("GetLobbyMembers", _currentLobby!.LobbyId);

                OnLog?.Invoke($"Lobby found! Id: {member.Puuid}");
                OnLog?.Invoke($"Lobby id: {member.Puuid}");
                OnLog?.Invoke($"Members count: {members.Count}");
                OnLog?.Invoke("Lobby status: connected");
                LobbyJoined?.Invoke(_currentLobby);

                lobbyFound = true;
            }
        }

        if (lobbyFound == false)
        {
            OnLog?.Invoke("Lobby not found, creating...");
            await _hubConnection!.InvokeAsync("CreateLobby", lobby.LocalMember.Puuid, lobby.LocalMember.Puuid);

            _currentLobby!.LobbyId = lobby.LocalMember.Puuid;

            var members = await _hubConnection!.InvokeAsync<List<LobbyMember>>("GetLobbyMembers", _currentLobby!.LobbyId);
            OnLog?.Invoke("Lobby status: created");
            OnLog?.Invoke($"Lobby id: {lobby.LocalMember.Puuid}");
            LobbyJoined?.Invoke(_currentLobby);
        }

    }
    private async void OnGameFlowChanged(object? sender, LeagueEvent e)
    {
        var data = e.Data.ToString();

        OnLog?.Invoke($"GameFlowStatus: {data}");

        if (string.IsNullOrEmpty(data))
            return;

        if (data == "Lobby")
        {
            _currentLobby = await _lcuService.ExtractLeagueLobbyAsync();
            await ConnectToLobbyAsync(_currentLobby);
            return;
        }
        else if (data == "None")
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.InvokeAsync("LeaveLobby");

                    await _hubConnection.StopAsync();
                    OnLog?.Invoke("Lobby status: disconnected");
                    LobbyLeaved?.Invoke();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to leave SignalR lobby: {ex.Message}");
            }
        }
    }
    private async void OnChampionSelected(object? sender, LeagueEvent e)
    {
        SelectedChampionId = e.Data.ToString();
        var champion = _champions.FirstOrDefault(x => x.Id.ToString().Equals(SelectedChampionId));
        if (champion == null) return;
        if (SelectedSkins.ContainsKey(champion) == false)
            return;
        _selectedSkin = SelectedSkins[champion];
        await SendSkinDataToPartyAsync(_selectedSkin);
        SkinSended?.Invoke(_selectedSkin);
    }
    public async Task SendSkinDataToPartyAsync(Skin skin)
    {
        if (_hubConnection == null) return;

        var champion = _champions.FirstOrDefault(c => c.Skins.Contains(skin));
        if (champion == null) return;
        if (champion.Id.ToString().Equals(SelectedChampionId) == false)
            return;

        try
        {
            OnLog?.Invoke("Sending skin: " + skin.Name);
            await _hubConnection.InvokeAsync("SendMessage", _currentLobby!.LobbyId,
                JsonConvert.SerializeObject(skin));
            OnLog?.Invoke("Sended successfully");

        }
        catch (Exception ex)
        {
            OnError?.Invoke("Error applying skin: " + ex.Message);
        }
    }
}
