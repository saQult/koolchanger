using KoolChanger.Models;
using System.Collections.Generic;
using KoolChanger.ClientMvvm.Helpers;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface IConfigService
{
    Config LoadConfig();
    void SaveConfig(Config config);
    void SaveSelectedSkins(Config config, Dictionary<Champion, Skin> selectedSkins);
    void InitializeGamePath(Config config);
    Dictionary<Champion, Skin> LoadSelectedSkins(List<Champion> allChampions);
}