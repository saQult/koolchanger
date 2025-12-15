using System.IO;
using KoolChanger.Client.Helpers;
using KoolChanger.Client.Interfaces;
using KoolChanger.Core.Helpers;
using KoolChanger.Core.Models;
using Newtonsoft.Json;

namespace KoolChanger.Client.Services;

public class ConfigService : IConfigService
{
    private const string ConfigFileName = "config.json";

    public Config LoadConfig()
    {
        if (!File.Exists(ConfigFileName)) 
            return new Config();

        try
        {
            var json = File.ReadAllText(ConfigFileName);
            var config = JsonConvert.DeserializeObject<Config>(json);
            return config ?? new Config();
        }
        catch
        {
            return new Config();
        }
    }

    public void SaveConfig(Config config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFileName, json);
        }
        catch (Exception)
        {
        }
    }

    public void SaveSelectedSkins(Config config, Dictionary<Champion, Skin> selectedSkins)
    {
        config.SelectedSkins = selectedSkins.ToDictionary(k => k.Key.Name, v => v.Value);
        SaveConfig(config);
    }

    public Dictionary<Champion, Skin> LoadSelectedSkins(List<Champion> allChampions)
    {
        var config = LoadConfig();
        
        return config.SelectedSkins
            .Select(pair =>
            {
                var champ = allChampions.FirstOrDefault(c => c.Name == pair.Key);
                return champ != null ? new KeyValuePair<Champion, Skin>(champ, pair.Value) : default;
            })
            .Where(kv => kv.Key != null)
            .ToDictionary(kv => kv.Key!, kv => kv.Value);
    }

    public void InitializeGamePath(Config config)
    {
        if (!Directory.Exists(config.GamePath))
        {
            var path = RiotPathDetector.GetLeaguePath();
            if (!string.IsNullOrEmpty(path))
            {
                config.GamePath = path;
            }
        }
    }
}