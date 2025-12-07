#region

using System.Text.RegularExpressions;
using KoolChanger.Helpers;
using ManagedWrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace KoolChanger.Services;

public class UpdateService
{
    public event Action<string>? OnUpdating;
    private readonly ChampionService _championService = new();
    private readonly SkinService _skinService = new();
    private readonly WadExtractor _extractor = new();
    private readonly RitoBin _ritoBin = new();


    public async Task GenerateSkins()
    {
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp"));
        try
        {
            var gameSkins = await _championService.GetChampionsAsync();
            await _skinService.GetAllSkinsAsync(gameSkins);

            // 💡 Использование List<Task> вместо пустого массива
            var tasks = new List<Task>();
            var tasks1 = new List<Task>();
            foreach (var champion in gameSkins)
                if (champion.Name == "Annie")
                    tasks1.Add(Task.Run(async () =>
                    {
                        _extractor.Extract(
                            Path.Combine(RiotPathDetector.GetLeaguePath() ?? throw new InvalidOperationException(),
                                "DATA",
                                "FINAL", "Champions", champion.Name + ".wad.client"),
                            Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".extracted"),
                            Path.Combine(AppContext.BaseDirectory, "hashes", "hashes.game.txt"));

                            
                        foreach (var charactersDirectory in Directory.GetDirectories(Path.Combine(Path.GetTempPath(),
                                     "KoolChanger.tmp", champion.Name + ".extracted",
                                     "data", "characters")).Select(d => new DirectoryInfo(d).Name))
                            
                        foreach (var skinPath in Directory.GetFiles(Path.Combine(Path.GetTempPath(),
                                     "KoolChanger.tmp",
                                     champion.Name + ".extracted",
                                     "data", "characters", charactersDirectory, "skins")))
                        {
                            // if(charactersDirectory.Contains("annietibbers")) continue;
                            // Console.WriteLine(skinPath);
                            if(!skinPath.Contains("skin28")) continue;
                            
                            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp",
                                champion.Name + ".completed", Path.GetFileNameWithoutExtension(skinPath), "data",
                                "characters", charactersDirectory));
                            _ritoBin.ConvertBintoJson(
                                skinPath,
                                Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".completed",
                                    Path.GetFileNameWithoutExtension(skinPath), "data", "characters",
                                    charactersDirectory, "skins", "skin0.json"),
                                Path.Combine(AppContext.BaseDirectory, "hashes"));
                            
                            var ParsedJson = JObject.Parse(File.ReadAllText( Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".completed",
                                Path.GetFileNameWithoutExtension(skinPath), "data", "characters",
                                charactersDirectory, "skins", "skin0.json")));
                            var ab = ParsedJson["entries"]["value"]["items"][0];
                            ab["key"] = $"Characters/{charactersDirectory}/Skins/Skin0";
                            var n = ParsedJson["entries"]["value"]["items"][0]["value"]["items"].Where(x =>
                            {
                                var value = x.Value<string>("key");
                                return value == "mResourceResolver";
                            }).FirstOrDefault()["value"] = $"Characters/{charactersDirectory}/Skins/Skin0/Resources";
                            var m = ParsedJson["entries"]["value"]["items"][1];
                            m["key"] = $"Characters/{charactersDirectory}/Skins/Skin0/Resources";
                            
                            File.WriteAllText(Path.Combine(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".completed",
                                Path.GetFileNameWithoutExtension(skinPath), "data", "characters",
                                charactersDirectory, "skins", "skin0.json")), JsonConvert.SerializeObject(ParsedJson));
                            
                            
                            _ritoBin.ConvertJsonToBin(Path.Combine(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".completed",
                                    Path.GetFileNameWithoutExtension(skinPath), "data", "characters",
                                    charactersDirectory, "skins", "skin0.json")),
                                Path.Combine(Path.Combine(Path.GetTempPath(), "KoolChanger.tmp", champion.Name + ".completed",
                                    Path.GetFileNameWithoutExtension(skinPath), "data", "characters",
                                    charactersDirectory, "skins", "skin0.bin")),
                                Path.Combine(AppContext.BaseDirectory, "hashes")
                                
                                );
                        }
                        
                    }));

            await Task.WhenAll(tasks1);

            Console.WriteLine($"Prepared Tasks list: {tasks.Count} tasks");

            await Task.WhenAll(tasks);
            Console.WriteLine("Complited!");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    // public Task ChangeSkinId(JObject skin)
    // {
    //     
    // }

    // made by random guy from lolru discord
    private static class NameRules
    {
        private static readonly Regex RX_WIN_FORBIDDEN = new(@"[\\/:*?""<>|]+", RegexOptions.Compiled);
        private static readonly Regex RX_NON_WORD = new(@"[^A-Za-z0-9_\-\s]+", RegexOptions.Compiled);
        private static readonly Regex RX_SPACES = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RX_TRIM_EDGES = new(@"^[_\-]+|[_\-]+$", RegexOptions.Compiled);

        public static string SanitizeForPath(string name, string fallback = "item")
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;

            var s = RX_WIN_FORBIDDEN.Replace(name, " ");
            s = RX_NON_WORD.Replace(s, " ");
            s = RX_SPACES.Replace(s, "_");
            s = RX_TRIM_EDGES.Replace(s, "");

            if (string.IsNullOrEmpty(s)) s = fallback;

            return s;
        }
    }
}