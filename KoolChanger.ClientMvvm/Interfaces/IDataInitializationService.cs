using KoolChanger.Models;
using KoolChanger.ClientMvvm.Helpers;
using KoolChanger.ClientMvvm.ViewModels;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface IDataInitializationService
{
    List<Champion> AllChampions { get; }
    event Action<string>? OnUpdating;

    Task InitializeDataAsync(Config config);
    Task<List<SkinViewModel>> LoadChampionSkinsAsync(Champion champion, Dictionary<Champion, Skin> selectedSkins);
}