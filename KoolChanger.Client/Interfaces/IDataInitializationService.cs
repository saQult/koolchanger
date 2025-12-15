using KoolChanger.Core.Models;
using KoolChanger.Client.Helpers;
using KoolChanger.Client.ViewModels;

namespace KoolChanger.Client.Interfaces;

public interface IDataInitializationService
{
    List<Champion> AllChampions { get; }
    event Action<string>? OnUpdating;

    Task InitializeDataAsync(Config config);
    Task<List<SkinViewModel>> LoadChampionSkinsAsync(Champion champion, Dictionary<Champion, Skin> selectedSkins);
}