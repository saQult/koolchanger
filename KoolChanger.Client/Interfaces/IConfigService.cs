using KoolChanger.Core.Models;
using KoolChanger.Client.Helpers;

namespace KoolChanger.Client.Interfaces;

public interface IConfigService
{
    Config LoadConfig();
    void SaveConfig(Config config);
    void SaveSelectedSkins(Config config, Dictionary<Champion, Skin> selectedSkins);
    void InitializeGamePath(Config config);
    Dictionary<Champion, Skin> LoadSelectedSkins(List<Champion> allChampions);
}