using CSLOLTool.Dto;
using CSLOLTool.Models;
using System.Net.Http;
using System.Text.Json;

namespace CSLOLTool.Services;

public class ChampionService
{
    private readonly string _championsInfoEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champions/";
    private readonly string _championsSummaryEndpoiunt = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json";
    private static readonly HttpClient _httpClient = new HttpClient();

    public async Task<List<Champion>> GetChampionsAsync()
    { 
        string json = await _httpClient.GetStringAsync(_championsSummaryEndpoiunt);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var champions = new List<Champion>();

        foreach (var element in root.EnumerateArray())
        {
            int id = element.GetProperty("id").GetInt32();
            if(id < 0 ) continue;
            string name = element.GetProperty("name").GetString() ?? "Unknown";

            champions.Add(new Champion
            {
                Id = id,
                Name = name,
            });
        }

        return champions;
    }
}
