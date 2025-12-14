using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Helpers; // Для RelayCommand внутри SkinViewModel
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.ViewModels; // Для SkinViewModel
using KoolChanger.Models;
using KoolChanger.Services; // Для ChampionService и SkinService
using Newtonsoft.Json;

namespace KoolChanger.ClientMvvm.Services;

public class DataInitializationService : IDataInitializationService
{
    private readonly ChampionService _championService;
    private readonly SkinService _skinService;
    private readonly INavigationService _navigationService; 

    public List<Champion> AllChampions { get; private set; } = new();
    public event Action<string>? OnUpdating;

    public DataInitializationService(ChampionService championService, SkinService skinService, INavigationService navigationService)
    {
        _championService = championService;
        _skinService = skinService;
        _navigationService = navigationService;
    }

    public async Task InitializeDataAsync(Config config)
    {
        await DownloadSplashesIfNeeded();

        await LoadChampionsData();

        await DownloadIcons();
    }

    private async Task DownloadSplashesIfNeeded()
    {
        var splashesPath = Path.Combine(new FileInfo(Environment.ProcessPath!).DirectoryName!, "assets", "champions", "splashes");
        if (Directory.GetFiles(splashesPath).Length == 0)
        {
            var result = _navigationService.ShowCustomMessageBox("Info",
                "Do you want to download preview for champion skins now? " +
                "If not, previews will download in real time when you select any champion").DialogResult;
            
            if (result == true) 
            {
                _championService.OnDownloaded += message => OnUpdating?.Invoke(message);
                await _championService.DownloadAllPreviews();
            }
        }
    }

    private async Task LoadChampionsData()
    {
        try
        {
            if (File.Exists("champion-data.json"))
            {
                var json = await File.ReadAllTextAsync("champion-data.json");
                var data = JsonConvert.DeserializeObject<List<Champion>>(json);
                
                if (data != null && data.Count > 0)
                {
                    AllChampions = data;
                }
                else
                {
                    AllChampions = await FetchFromApi();
                }
            }
            else
            {
                AllChampions = await FetchFromApi();
            }
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke($"Error loading data: {ex.Message}");
            AllChampions = await FetchFromApi();
        }
    }

    private async Task<List<Champion>> FetchFromApi()
    {
        OnUpdating?.Invoke("Getting champions info...");
        var champions = await _championService.GetChampionsAsync();
        var result = await _skinService.GetAllSkinsAsync(champions);
        
        result.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        
        OnUpdating?.Invoke("Finished getting info");
        await File.WriteAllTextAsync("champion-data.json", JsonConvert.SerializeObject(result));
        
        return result;
    }

    private async Task DownloadIcons()
    {
        var semaphore = new SemaphoreSlim(50);
        var tasks = new List<Task>();

        foreach (var champion in AllChampions)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var iconPath = Path.Combine("assets", "champions", $"{champion.Id}.png");
                    if (!File.Exists(iconPath))
                    {
                        OnUpdating?.Invoke($"Downloading icon for {champion.Name}");
                        await _championService.DownloadChampionIconAsync(champion.Id, Path.Combine("assets", "champions"));
                    }
                }
                catch
                {
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        await Task.WhenAll(tasks);
    }

    public async Task<List<SkinViewModel>> LoadChampionSkinsAsync(Champion selectedChamp, Dictionary<Champion, Skin> selectedSkins)
    {
        var resultSkins = new List<SkinViewModel>();

        foreach (var skin in selectedChamp.Skins.Skip(1))
        {
            await EnsureSkinPreviewAsync(skin);

            var skinVm = new SkinViewModel
            {
                Id = skin.Id,
                Name = skin.Name,
                ImageUrl = Path.Combine(new FileInfo(Environment.ProcessPath!).DirectoryName!, "assets", "champions", "splashes", $"{skin.Id}.png"),
                Model = skin,
                Champion = selectedChamp,
                IsSelected = IsSkinSelected(selectedSkins, selectedChamp, skin.Id)
            };

            if (skin.Chromas.Count > 0)
            {
                skinVm.HasChromas = true;
                var parentSkinVm = skinVm;

                foreach (var chroma in skin.Chromas)
                {
                    var chromaPath = Path.Combine(new FileInfo(Environment.ProcessPath!).DirectoryName!, "assets", "champions", "splashes", $"{chroma.Id}.png");
                    if (!File.Exists(chromaPath))
                    {
                        await _championService.DownloadImageAsync(chroma.ImageUrl, chromaPath);
                    }

                    var chromaVm = new SkinViewModel
                    {
                        Id = chroma.Id,
                        Name = chroma.Name,
                        ImageUrl = chromaPath,
                        Color = chroma.Colors.FirstOrDefault() ?? "#FFFFFF",
                        Model = chroma,
                        Champion = selectedChamp,
                        IsSelected = IsSkinSelected(selectedSkins, selectedChamp, chroma.Id),
                        IsChroma = true,
                        Parent = parentSkinVm
                    };

                    parentSkinVm.Children.Add(chromaVm);
                }
            }

            var skinIdStr = skin.Id.ToString();
            var champIdStr = selectedChamp.Id.ToString();
            
            if (skinIdStr.StartsWith(champIdStr))
            {
                var skinIdShort = skinIdStr.Substring(champIdStr.Length);
                var specialFormsPath = Path.Combine("skins", $"{selectedChamp.Id}", "special_forms", $"{skinIdShort}");

                if (Directory.Exists(specialFormsPath))
                {
                    var sortedForms = Directory.GetFiles(specialFormsPath)
                        .OrderBy(x => 
                        {
                            int.TryParse(Path.GetFileNameWithoutExtension(x), out int val);
                            return val;
                        }).ToList();

                    foreach (var file in sortedForms)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var formImage = Path.Combine(new FileInfo(Environment.ProcessPath!).DirectoryName!, specialFormsPath, "models_image", $"{name}.png");

                        SkinForm formSkinModel = new()
                        {
                            Id = skin.Id,
                            Name = skin.Name,
                            ImageUrl = skin.ImageUrl,
                            Chromas = skin.Chromas,
                            Stage = name
                        };

                        skinVm.Children.Add(new SkinViewModel
                        {
                            Id = skin.Id,
                            Name = name,
                            ImageUrl = formImage,
                            Model = formSkinModel,
                            Champion = selectedChamp,
                            IsSelected = IsSkinSelectedForm(selectedSkins, selectedChamp, skin.Id, name),
                            IsForm = true
                        });
                    }
                }
            }

            resultSkins.Add(skinVm);
        }

        return resultSkins;
    }

    private async Task EnsureSkinPreviewAsync(Skin skin)
    {
        var path = Path.Combine(new FileInfo(Environment.ProcessPath!).DirectoryName!, "assets", "champions", "splashes", $"{skin.Id}.png");
        if (!File.Exists(path))
        {
            await _championService.DownloadImageAsync(skin.ImageUrl, path);
        }
    }

    private bool IsSkinSelected(Dictionary<Champion, Skin> selectedSkins, Champion champ, long skinId)
    {
        return selectedSkins.TryGetValue(champ, out var s) && s.Id == skinId && !(s is SkinForm);
    }

    private bool IsSkinSelectedForm(Dictionary<Champion, Skin> selectedSkins, Champion champ, long skinId, string stage)
    {
        return selectedSkins.TryGetValue(champ, out var s) && s.Id == skinId && s is SkinForm f && f.Stage == stage;
    }
}