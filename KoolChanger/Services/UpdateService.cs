#region

using System.IO.Compression;
using System.Text.RegularExpressions;
using KoolChanger.Helpers;
using KoolChanger.Models;
using KoolWrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KoolWrapper;
#endregion

namespace KoolChanger.Services;

public class UpdateService
{
    public event Action<string>? OnUpdating;

    private readonly ChampionService _championService = new();
    private readonly SkinService _skinService = new();

    private readonly RitoBin _ritoBin = new();

    private const string TempDirName = "KoolChanger.tmp";
    private const string DataDir = "data";
    private const string CharactersDir = "characters";
    private const string SkinsDir = "skins";
    private const string HashesDir = "hashes";
    private const string HashesGameFile = "hashes.game.txt";
    private const string Skin0Json = "skin0.json";
    private const string Skin0Bin = "skin0.bin";


    private static readonly Dictionary<string, string> ChampionNormalize = new()
    {
        { "Xin Zhao", "XinZhao" },
        { "Kai'Sa", "Kaisa" },
        { "Vel'Koz", "Velkoz" },
        { "Kha'Zix", "Khazix" },
        { "Cho'Gath", "Chogath" },
        { "Rek'Sai", "Reksai" },
        { "Bel'Veth", "Belveth" },
        { "Kog'Maw", "Kogmaw" },
        { "Jarvan IV", "JarvanIV" },
        { "Master Yi", "MasterYi" },
        { "Miss Fortune", "MissFortune" },
        { "Lee Sin", "LeeSin" },
        { "Tahm Kench", "TahmKench" },
        { "Twisted Fate", "TwistedFate" },
        { "Aurelion Sol", "AurelionSol" },
        { "Dr. Mundo", "DrMundo" },
        { "Wukong", "MonkeyKing" },
        { "RenataGlasc", "Renata" },
        { "Nunu&Willump", "Nunu" }
    };



    private static string BaseTempPath => Path.Combine(Path.GetTempPath(), TempDirName);

    private static string BaseAppPath => new FileInfo(Environment.ProcessPath).DirectoryName;


    public async Task GenerateSkins()
    {
        OnUpdating?.Invoke("Starting skin generation process...");
        Directory.CreateDirectory(BaseTempPath);

        try
        {
            OnUpdating?.Invoke("Fetching champion and skin data...");
            var gameSkins = await _championService.GetChampionsAsync();
            await _skinService.GetAllSkinsAsync(gameSkins);

            OnUpdating?.Invoke($"Preparing tasks for {gameSkins.Count} champions...");

            var processingTasks = gameSkins.Select(ProcessChampionSkinsAsync).ToList();
            await Task.WhenAll(processingTasks);

            OnUpdating?.Invoke("All champions processed successfully!");
            Console.WriteLine("Completed!");
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke($"An error occurred: {ex.Message}");
            Console.WriteLine(ex);
        }
        finally
        {
            // TODO clean directories
        }
    }

    private async Task ProcessChampionSkinsAsync(Champion champion)
    {
        champion.Name = NormalizeChampionName(champion.Name);
        OnUpdating?.Invoke($"Processing {champion.Name}...");
        var championTempPath = Path.Combine(BaseTempPath, champion.Name + ".extracted");

        try
        {
            OnUpdating?.Invoke($"Extracting WAD for {champion.Name}...");
            // todo from config
            var leaguePath = RiotPathDetector.GetLeaguePath() ??
                             throw new InvalidOperationException("League of Legends path not found.");

            var wadPath = Path.Combine(leaguePath, "DATA", "FINAL", "Champions", champion.Name + ".wad.client");
            var hashPath = Path.Combine(BaseAppPath, HashesDir, HashesGameFile);

            WadExtractor.extract(wadPath, championTempPath, hashPath);

            var charactersRoot = Path.Combine(championTempPath, DataDir, CharactersDir);

            if (!Directory.Exists(charactersRoot))
            {
                OnUpdating?.Invoke($"Warning: No character data found for {champion.Name} at {charactersRoot}");
                return;
            }

            var skinProcessingTasks = Directory.GetDirectories(charactersRoot)
                .Select(d => new DirectoryInfo(d).Name) 
                .SelectMany(characterName =>
                    GetSkinBinPaths(charactersRoot, characterName)
                        .Select(skinBinPath => ProcessSingleSkinAsync(champion, characterName, skinBinPath))
                ).ToList();

            await Task.WhenAll(skinProcessingTasks);
            OnUpdating?.Invoke($"{champion.Name} processing complete.");
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke($"Error processing {champion.Name}: {ex.Message}");
        }
    }

    private static IEnumerable<string> GetSkinBinPaths(string charactersRoot, string characterName)
    {
        var skinsFolder = Path.Combine(charactersRoot, characterName, SkinsDir);
        return Directory.Exists(skinsFolder)
            ? Directory.GetFiles(skinsFolder, "*.bin")
            : Enumerable.Empty<string>();
    }

    private async Task ProcessSingleSkinAsync(Champion champion, string characterName, string skinBinPath)
    {
        var skinFileName = Path.GetFileNameWithoutExtension(skinBinPath);
        var championCompletedPath = Path.Combine(BaseTempPath, champion.Name + ".completed");
        var skinCompletedPath = Path.Combine(championCompletedPath, skinFileName);
        var targetCharacterPath = Path.Combine(skinCompletedPath, DataDir, CharactersDir, characterName);
        var targetSkinsPath = Path.Combine(targetCharacterPath, SkinsDir);
        
        Directory.CreateDirectory(targetSkinsPath);

        var targetJsonPath = Path.Combine(targetSkinsPath, Skin0Json);
        var targetBinPath = Path.Combine(targetSkinsPath, Skin0Bin);
        var hashesPath = Path.Combine(BaseAppPath, HashesDir);
        var newSkinKey = $"Characters/{characterName}/Skins/Skin0";
        var newResourcesKey = $"{newSkinKey}/Resources";

        OnUpdating?.Invoke($"Modifying skin: {characterName}/{skinFileName}");

        try
        {
            RitoBin.ConvertBintoJson(skinBinPath, targetJsonPath, hashesPath);

            var parsedJson = JObject.Parse(await File.ReadAllTextAsync(targetJsonPath));
            ModifySkinJson(parsedJson, newSkinKey, newResourcesKey);

            var modifiedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            await File.WriteAllTextAsync(targetJsonPath, modifiedJson);

            RitoBin.ConvertJsonToBin(targetJsonPath, targetBinPath, hashesPath);

            if (File.Exists(targetJsonPath))
                File.Delete(targetJsonPath);

            var wadOutputPath = Path.Combine(championCompletedPath, $"{skinFileName}");
            WadExtractor.pack(wadOutputPath, "");

            
            JObject info = new JObject
            {
                ["Author"] = "krutie.pw innovations LLC",
                ["Description"] = "Imported using KoolWrapper",
                ["Heart"] = "",
                ["Home"] = "",
                ["Name"] = $"{characterName} {skinFileName}", 
                ["Version"] = "1.0.0"
            };

            CreateSkinArchive(
                champion.Name,
                skinFileName,
                Path.ChangeExtension(wadOutputPath, "wad.client"),
                info);
            
            OnUpdating?.Invoke($"Skin {characterName}/{skinFileName} packed into WAD.");
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke($"Error processing skin {characterName}/{skinFileName}: {ex.Message}");
        }
    }
    

    private void CreateSkinArchive(
        string championName,
        string skinFileName,        
        string wadFilePath,         
        JObject infoJson            
    )
    {
        string archiveName = $"{skinFileName}.zip";

        string destBase = Path.Combine(new FileInfo(Environment.ProcessPath).DirectoryName, "skins", championName);
        Directory.CreateDirectory(destBase);

        string archivePath = Path.Combine(destBase, archiveName);

        string temp = Path.Combine(Path.GetTempPath(), $"KC_ARCHIVE_{Guid.NewGuid()}");
        string metaDir = Path.Combine(temp, "META");
        string wadDir  = Path.Combine(temp, "WAD");

        Directory.CreateDirectory(metaDir);
        Directory.CreateDirectory(wadDir);

        File.WriteAllText(
            Path.Combine(metaDir, "info.json"),
            infoJson.ToString(Newtonsoft.Json.Formatting.Indented)
        );

        File.Copy(wadFilePath, Path.Combine(wadDir, $"{championName}.wad.client"), true);
            
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        ZipFile.CreateFromDirectory(temp, archivePath, CompressionLevel.Optimal, false);

        Directory.Delete(temp, true);
    }


    private static void ModifySkinJson(JObject parsedJson, string newSkinKey, string newResourcesKey)
    {
        var items = parsedJson["entries"]?["value"]?["items"] as JArray;
        if (items == null) return;
        
        var skinDataProperties = items.FirstOrDefault(x =>
            x["value"]?.Value<string>("name") == "SkinCharacterDataProperties");
        if (skinDataProperties != null)
        {
            skinDataProperties["key"] = newSkinKey;
        }

        var resourceResolver = items.FirstOrDefault(x =>
            x["value"]?.Value<string>("name") == "ResourceResolver");
        if (resourceResolver != null)
        {
            resourceResolver["key"] = newResourcesKey;
        }
        
        var firstEntryItems = parsedJson["entries"]?["value"]?["items"]?[0]?["value"]?["items"] as JArray;
        if (firstEntryItems != null)
        {            
            var resourceResolverEntry = firstEntryItems.FirstOrDefault(x => 
                x.Value<string>("key") == "mResourceResolver");

            if (resourceResolverEntry != null)
            {
                resourceResolverEntry["value"] = newResourcesKey;
            }
        }
        
        
    }
    private static string NormalizeChampionName(string name)
    {
        return ChampionNormalize.TryGetValue(name, out var fixedName)
            ? fixedName
            : name.Replace(" ", "").Replace("'", "").Replace(".", "");
    }
    
    // The NameRules class has been kept as a private nested utility class for path sanitization.
    // Since it's not currently used in the refactored GenerateSkins, I'm keeping it as-is 
    // but without any modifications as the original intent was not clear.
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