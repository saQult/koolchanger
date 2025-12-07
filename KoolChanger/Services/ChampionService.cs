using KoolChanger.Dto;
using System.Net.Http;
using System.Text.Json;
using KoolChanger.Models;

namespace KoolChanger.Services;

public class ChampionService
{
    private const string ChampionsSummaryEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-summary.json";
    private const string ChampionsIconsEndpoint = "https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champion-icons/";
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly SkinService _skinService = new();
    public event Action<string>? OnDownloaded;
    
    public async Task<List<Champion>> GetChampionsAsync()
    { 
        string json = await _httpClient.GetStringAsync(ChampionsSummaryEndpoint);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var champions = new List<Champion>();

        foreach (var element in root.EnumerateArray())
        {
            int id = element.GetProperty("id").GetInt32();
            if(id < 0) continue;
            string name = element.GetProperty("name").GetString() ?? "Unknown";

            if (name.ToLower().Contains("doom")) continue;

            champions.Add(new Champion
            {
                Id = id,
                Name = name,
            });
        }

        return champions;
    }
    public async Task DownloadChampionIconAsync(int id, string outputFolder)
    {
        string fileName = $"{id}.png";
        string url = ChampionsIconsEndpoint + fileName;
        string localPath = Path.Combine(outputFolder, fileName);
        await DownloadImageAsync(url, localPath);
    }
    public async Task DownloadImageAsync(string url, string output)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var imageData = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(output, imageData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    public async Task DownloadAllPreviews()
    {
        var champions = await _skinService.GetAllSkinsAsync(await GetChampionsAsync());
        champions.Sort((x, y) => x.Name.CompareTo(y.Name));


        var semaphore = new SemaphoreSlim(100);
        var tasks = new List<Task>();
        int done = 0;
        int total = champions.Sum(c => c.Skins.Count + c.Skins.Sum(s => s.Chromas.Count));

        foreach (var champion in champions)
        {
            foreach (var skin in champion.Skins)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var skinImagePath = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", skin.Id + ".png");
                        OnDownloaded?.Invoke($"[{Interlocked.Increment(ref done)}/{total}] Downloading image of " + skin.Name);
                        await DownloadImageAsync(skin.ImageUrl, skinImagePath);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                foreach (var chroma in skin.Chromas)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var chromaImagePath = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", chroma.Id + ".png");
                            OnDownloaded?.Invoke($"[{Interlocked.Increment(ref done)}/{total}] Downloading chroma of " + skin.Name);
                            await DownloadImageAsync(chroma.ImageUrl, chromaImagePath);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
            }
        }

        await Task.WhenAll(tasks);
    }
}
