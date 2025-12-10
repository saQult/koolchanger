#region

using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.Services;
using KoolChanger.Services;
using Newtonsoft.Json;

#endregion

namespace KoolChanger.ClientMvvm.ViewModels.Dialogs;

public class SettingsViewModel : ObservableObject
{
    private readonly ChampionService _championService;
    private readonly SkinService _skinService;

    private readonly UpdateService _updateService;

    private string _gamePath;

    private bool _isBusy;
    private string _status;

    public SettingsViewModel(IConfigService configService, UpdateService updateService, SkinService skinService,
        ChampionService championService, INavigationService navigationService)
    {
        _gamePath = configService.LoadConfig().GamePath;
        _updateService = updateService;
        _skinService = skinService;
        _championService = championService;

        CloseCommand = new RelayCommand(() => navigationService.CloseWindow(this));
        SelectGameFolderCommand = new RelayCommand(SelectGameFolder);
        DownloadSkinsCommand = new RelayCommand(() => StartGenerating(navigationService), CanExecute);
        GetChampionDataCommand = new RelayCommand(async void () => await GetChampionData(), () => CanExecute());
        DownloadSkinsPreviewCommand =
            new RelayCommand(async void () => await DownloadSkinsPreview(), () => CanExecute());

        _updateService.OnUpdating += message => Status = message;
        _skinService.OnDownloaded += message => Status = message;
        _championService.OnDownloaded += message => Status = message;
    }

    public ICommand CloseCommand { get; }
    public ICommand SelectGameFolderCommand { get; }
    public ICommand DownloadSkinsCommand { get; }
    public ICommand GetChampionDataCommand { get; }
    public ICommand DownloadSkinsPreviewCommand { get; }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            SetProperty(ref _isBusy, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event Action<string> GamePathChanged;

    private bool CanExecute()
    {
        return !IsBusy;
    }

    private async Task DownloadSkins()
    {
        IsBusy = true;
        await _updateService.GenerateSkins();
        Status = "Finished downloading skins";
        IsBusy = false;
    }
    private void StartGenerating(INavigationService nav)
    {
        // Закрываем окно сразу
        nav.CloseWindow(this);

        // Убираем await — процесс идёт в фоне
        Task.Run(() => _updateService.GenerateSkins());
    }

    private async Task GetChampionData()
    {
        IsBusy = true;
        var champions = await _skinService.GetAllSkinsAsync(await _championService.GetChampionsAsync());
        champions.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        await File.WriteAllTextAsync("champion-data.json", JsonConvert.SerializeObject(champions));
        Status = "Finished getting info";
        IsBusy = false;
    }

    private async Task DownloadSkinsPreview()
    {
        IsBusy = true;
        await _championService.DownloadAllPreviews();
        Status = "Finished downloading champion previews";
        IsBusy = false;
    }

    private void SelectGameFolder()
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select the folder where League of Legends is installed";
        dialog.UseDescriptionForTitle = true;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _gamePath = dialog.SelectedPath;
            GamePathChanged?.Invoke(_gamePath);
        }
    }
}