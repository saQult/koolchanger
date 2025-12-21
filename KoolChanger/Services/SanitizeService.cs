using KoolChanger.Models;

namespace KoolChanger.Services;

public class SanitizeService
{
    
    Dictionary<string, string> _championNormalize = new()
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
        { "Renata Glasc", "Renata" },
        { "Nunu & Willump", "Nunu" }
    };
    public string NormalizeChampionName(string name)
    {
        return _championNormalize.TryGetValue(name, out var fixedName)
            ? fixedName
            : name.Replace(" ", "").Replace("'", "").Replace(".", "");
    }
    public Champion SanitazeChampion(Champion c)
    {
        c.Name = NormalizeChampionName(c.Name);
        return c;
    }
}