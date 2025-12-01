using LCUSharp;
using LCUSharp.Websocket;
using System.Text.Json;
using KoolChanger.Models;

namespace KoolChanger.Services
{
    public class LCUService
    {
        public event EventHandler<LeagueEvent>? GameFlowChanged;
        public event EventHandler<LeagueEvent>? ChampionSelected;
        public LeagueClientApi? Api;

        public readonly string GameFlowEndpoint = "/lol-gameflow/v1/gameflow-phase";
        public readonly string CurrentChampionEndpoint = "/lol-champ-select/v1/current-champion";

        public async Task ConnectAsync()
        {
            Api = await LeagueClientApi.ConnectAsync();
        }
        public void SubscrbeLobbyEvent()
        {
            Api?.EventHandler.Subscribe(GameFlowEndpoint, GameFlowChanged);
        }
        public void SubscrbeLChampionSelect()
        {
            Api?.EventHandler.Subscribe(CurrentChampionEndpoint, ChampionSelected);
        }
        public async Task<string> GetGameFlowPhaseAsync()
        {
            if (Api == null) 
                throw new NullReferenceException("League client does not connected, API is null");
            return await Api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, "/lol-gameflow/v1/gameflow-phase");
        }
        public async Task<LobbyData> ExtractLeagueLobbyAsync()
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
}
