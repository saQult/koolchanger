using CSLOLTool.Dto;
using CSLOLTool.Models;
using System;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;

namespace CSLOLTool.Services;

public class SkinService
{
    private readonly string _championsInfoEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champions/";
    private readonly string _splashArtEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/assets/characters/";
    private readonly string _chromaEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-chroma-images/";
    public event Action<string>? OnDownloaded;
    public event Action<string>? OnError;
    private readonly HttpClient _httpClient = new HttpClient();
    public SkinFromFileInfo? GetModInfoFromZip(string zipPath)
    {
        if (!File.Exists(zipPath))
            return null;

        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry("META/info.json");

        if (entry == null)
            return null;

        using var reader = new StreamReader(entry.Open());
        var json = reader.ReadToEnd();

        var modInfo = JsonSerializer.Deserialize<SkinFromFileInfo>(json);
        return modInfo;
    }
    public async Task<List<Skin>> GetSkinsAsync(int championId)
    {
        string json = await _httpClient.GetStringAsync(_championsInfoEndpoint + $"/{championId}.json");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var skins = new List<Skin>();

        if (root.TryGetProperty("skins", out var skinsElement))
        {
            string prefix = "/lol-game-data/assets/ASSETS/Characters/";

            foreach (var skinElement in skinsElement.EnumerateArray())
            {
                int id = skinElement.GetProperty("id").GetInt32();
                string name = skinElement.GetProperty("name").GetString() ?? "Unknown";
                string loadScreenPath = skinElement.GetProperty("loadScreenPath").GetString() ?? "";

                var imageUrl = loadScreenPath.StartsWith(prefix)
                    ? loadScreenPath.Substring(prefix.Length).ToLower()
                    : loadScreenPath.ToLower();

                var skin = new Skin
                {
                    Id = id,
                    Name = name,
                    ImageUrl = _splashArtEndpoint + imageUrl
                };
                Console.WriteLine(name);
                if (skinElement.TryGetProperty("chromas", out var chromasElement))
                {
                    foreach (var chromaElement in chromasElement.EnumerateArray())
                    {
                        var chromaId = chromaElement.GetProperty("id").GetInt32();
                        var chroma = new Chroma
                        {
                            Id = chromaId,
                            Name = chromaElement.GetProperty("name").GetString() ?? "",
                            ImageUrl = $"{_chromaEndpoint}{championId}/{chromaId}.png",
                            Colors = chromaElement.TryGetProperty("colors", out var colorsElement)
                                ? colorsElement.EnumerateArray().Select(c => c.GetString() ?? "").ToList()
                                : new List<string>()
                        };
                        
                        skin.Chromas.Add(chroma);
                    }
                }

                if (skinElement.TryGetProperty("questSkinInfo", out var questSkinInfo) &&
                    questSkinInfo.TryGetProperty("tiers", out var tiersElement))
                {
                    foreach (var tierElement in tiersElement.EnumerateArray())
                    {

                        var skinForm = new SkinForm
                        {
                            Id = tierElement.GetProperty("id").GetInt32(),
                            Name = tierElement.GetProperty("name").GetString() ?? "Unknown",
                            Stage = tierElement.GetProperty("stage").GetInt32(),
                            ImageUrl = _splashArtEndpoint + (
                                tierElement.GetProperty("loadScreenPath").GetString() ?? ""
                            ).Replace(prefix, "").ToLower()
                        };
                        Console.WriteLine(skinForm.Name);
                        skin.Forms.Add(skinForm);
                    }
                }

                skins.Add(skin);
            }
        }

        return skins;
    }
    public async Task<List<Champion>> GetAllSkinsAsync(List<Champion> champions)
    {
        var semaphore = new SemaphoreSlim(50);
        var tasks = new List<Task>();

        foreach (var champion in champions)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var skins = await GetSkinsAsync(champion.Id);
                    OnDownloaded?.Invoke($"Downloaded skins for {champion.Name}");
                    champion.Skins = skins;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Failed to get skins for {champion.Name}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        return champions;
    }

}
