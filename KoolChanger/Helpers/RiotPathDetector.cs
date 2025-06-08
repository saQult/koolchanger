using System.IO;
using System.Text.Json;

namespace KoolChanger.Helpers;

public static class RiotPathDetector
{
    //idk tested on some machines - it's all the same, should work xd

    private const string RiotConfigPath = @"C:\ProgramData\Riot Games\RiotClientInstalls.json";

    public static string? GetLeaguePath()
    {
        if (!File.Exists(RiotConfigPath))
            return null;

        var json = File.ReadAllText(RiotConfigPath);
        var data = JsonSerializer.Deserialize<RiotClientInstalls>(json);

        if (data?.associated_client != null)
        {
            foreach (var kv in data.associated_client)
            {
                var path =  kv.Key.TrimEnd('\\', '/');
                return Path.Combine(path, "Game");
            }
        }

        return null;
    }
    private record RiotClientInstalls(Dictionary<string, string> associated_client, string rc_default);
}


