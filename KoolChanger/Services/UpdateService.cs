#region

using System.Text.RegularExpressions;
using KoolChanger.Helpers;
using KoolChanger.Models;
using KoolWrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KoolWrapper;
#endregion

namespace KoolChanger.Services;

/// <summary>
/// Service responsible for managing the generation and modification of champion skin files.
/// </summary>
public class UpdateService
{
    public event Action<string>? OnUpdating;

    private readonly ChampionService _championService = new();
    private readonly SkinService _skinService = new();
    private readonly WadExtractor _extractor = new();
    private readonly RitoBin _ritoBin = new();
    // Directory constants for clarity 
    private const string TempDirName = "KoolChanger.tmp";
    private const string DataDir = "data";
    private const string CharactersDir = "characters";
    private const string SkinsDir = "skins";
    private const string HashesDir = "hashes";
    private const string HashesGameFile = "hashes.game.txt";
    private const string Skin0Json = "skin0.json";
    private const string Skin0Bin = "skin0.bin";
    
    /// <summary>
    /// Base temporary directory path used for extraction and processing.
    /// </summary>
    private static string BaseTempPath => Path.Combine(Path.GetTempPath(), TempDirName);
    
    /// <summary>
    /// Base application directory path where hash files are stored.
    /// </summary>
    private static string BaseAppPath => new FileInfo(Environment.ProcessPath).DirectoryName;


    /// <summary>
    /// Executes the full process of generating and modifying skin configuration files.
    /// </summary>
    public async Task GenerateSkins()
    {
        OnUpdating?.Invoke("Starting skin generation process...");
        Directory.CreateDirectory(BaseTempPath);
        
        try
        {
            // 1. Fetch initial data
            OnUpdating?.Invoke("Fetching champion and skin data...");
            var gameSkins = await _championService.GetChampionsAsync();
            await _skinService.GetAllSkinsAsync(gameSkins);

            // 2. Prepare and run concurrent processing tasks
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
            // Optional: Clean up the temporary directory if needed, 
            // but often left for debugging/post-mortem analysis in tools like this.
            // if (Directory.Exists(BaseTempPath)) Directory.Delete(BaseTempPath, true);
        }
    }

    /// <summary>
    /// Processes a single champion: extracts WAD, finds skins, and converts/modifies them.
    /// </summary>
    private async Task ProcessChampionSkinsAsync(Champion champion)
    {
        OnUpdating?.Invoke($"Processing {champion.Name}...");
        var championTempPath = Path.Combine(BaseTempPath, champion.Name + ".extracted");
        
        try
        {
            // 1. Extract WAD file
            OnUpdating?.Invoke($"Extracting WAD for {champion.Name}...");
            var leaguePath = RiotPathDetector.GetLeaguePath() ?? 
                             throw new InvalidOperationException("League of Legends path not found.");
                             
            var wadPath = Path.Combine(leaguePath, "DATA", "FINAL", "Champions", champion.Name + ".wad.client");
            var hashPath = Path.Combine(BaseAppPath, HashesDir, HashesGameFile);
            
            _extractor.extract(wadPath, championTempPath, hashPath);

            // 2. Find and process extracted skins
            var charactersRoot = Path.Combine(championTempPath, DataDir, CharactersDir);
            
            if (!Directory.Exists(charactersRoot))
            {
                OnUpdating?.Invoke($"Warning: No character data found for {champion.Name} at {charactersRoot}");
                return;
            }

            var skinProcessingTasks = Directory.GetDirectories(charactersRoot)
                .Select(d => new DirectoryInfo(d).Name) // Character folder names (e.g., 'Aatrox')
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
            // Propagate the exception or log it, depending on desired concurrency error handling
            // throw; 
        }
    }
    
    /// <summary>
    /// Gets all .bin skin files within a character's skins directory.
    /// </summary>
    private static IEnumerable<string> GetSkinBinPaths(string charactersRoot, string characterName)
    {
        var skinsFolder = Path.Combine(charactersRoot, characterName, SkinsDir);
        return Directory.Exists(skinsFolder) 
            ? Directory.GetFiles(skinsFolder, "*.bin") 
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Converts a skin BIN file to JSON, modifies the necessary values, and converts it back to BIN.
    /// </summary>
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
            // 1. Convert BIN to JSON
            _ritoBin.ConvertBintoJson(skinBinPath, targetJsonPath, hashesPath);

            // 2. Read and Modify JSON
            var parsedJson = JObject.Parse(await File.ReadAllTextAsync(targetJsonPath));
            ModifySkinJson(parsedJson, newSkinKey, newResourcesKey);

            // 3. Write Modified JSON
            var modifiedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            await File.WriteAllTextAsync(targetJsonPath, modifiedJson);

            // 4. Convert JSON back to BIN
            _ritoBin.ConvertJsonToBin(targetJsonPath, targetBinPath, hashesPath);
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke($"Error modifying skin {characterName}/{skinFileName}: {ex.Message}");
            // Log or handle the single skin failure without stopping the entire batch
        }
    }

    /// <summary>
    /// Modifies the critical paths within the skin's JSON structure.
    /// </summary>
    private static void ModifySkinJson(JObject parsedJson, string newSkinKey, string newResourcesKey)
    {
        var items = parsedJson["entries"]?["value"]?["items"] as JArray;
        if (items == null) return;
        
        // Find and update SkinCharacterDataProperties key
        var skinDataProperties = items.FirstOrDefault(x =>
            x["value"]?.Value<string>("name") == "SkinCharacterDataProperties");
        if (skinDataProperties != null)
        {
            skinDataProperties["key"] = newSkinKey;
        }

        // Find and update ResourceResolver key
        var resourceResolver = items.FirstOrDefault(x =>
            x["value"]?.Value<string>("name") == "ResourceResolver");
        if (resourceResolver != null)
        {
            resourceResolver["key"] = newResourcesKey;
        }
        
        // Find and update mResourceResolver value within the first entry's items
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