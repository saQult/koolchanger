﻿using CSLOLTool;
using CSLOLTool.Models;
using CSLOLTool.Services;
using KoolChanger.Helpers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace KoolChanger;

public partial class SettingsWindow : Window
{
    public event Action<string>? PathSelected;
    private readonly UpdateService _updateService = new();
    private ToolService _toolService;
    private SkinService _skinService = new();
    private ChampionService _championService = new();
    private string _gamePath = string.Empty;
    private Preloader _preloader = new();
    public SettingsWindow(string gamePath)
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
            {
                BlurOpacity = 100
            };
        };
        _gamePath = gamePath;
        _toolService = new(_gamePath);

        _updateService.OnUpdating += message => _preloader.SetStatus(message);

        _toolService.SkinInstalled += skinName =>
            _preloader.SetStatus($"Installing {skinName}\n{_toolService.InstalledSkins} / {_toolService.InstallSkinCount}");

        _toolService.ChromaInstalled += chromaName =>
            _preloader.SetStatus($"Installing {chromaName}\n{_toolService.InstalledChromas} / {_toolService.InstallChromaCount}");

        _skinService.OnDownloaded += (message) => _preloader.SetStatus(message);
        _championService.OnDownloaded += (message) => _preloader.SetStatus(message);

        Loaded += (_, _) => _preloader = new() { Owner = this };
        Closed += (_, _) => _preloader.Close();

    }
    private void ShowPreloader()
    {
        Effect = new BlurEffect { Radius = 10 };
        IsEnabled = false;
        _preloader.Show();
    }
    private void HidePreloader()
    {
        Effect = null;
        IsEnabled = true;
        _preloader.Hide();
    }
    private async void DownloadSkins(object sender, RoutedEventArgs e)
    {
        ShowPreloader();
        await _updateService.DownloadSkins();
        HidePreloader();
        StatusLabel.Content = "Finished downloading skins";
    }
    private async void GetChampionData(object sender, RoutedEventArgs e)
    {
        ShowPreloader();
        
        var champions = await _skinService.GetAllSkinsAsync(await _championService.GetChampionsAsync());
        champions.Sort((x, y) => x.Name.CompareTo(y.Name));
        
        StatusLabel.Content = $"Finished getting info";

        File.WriteAllText("champion-data.json", JsonConvert.SerializeObject(champions));

        HidePreloader();
    }

    private void InstallSkins(object sender, RoutedEventArgs e)
    {
        var result = new CustomMessageBox("Warning", "Do you want to install all skins at once? \nIt will reduce time to apply skin but may take a lot of time and it will cost a lot of PC resourses", this) { Owner = this }.ShowDialog();

        if (result == false)
            return;

        ShowPreloader();
        _toolService.LoadBasicSkins(1);
        HidePreloader();
        StatusLabel.Content = "Skins installed";
    }

    private void InstallChromas(object sender, RoutedEventArgs e)
    {
        var result = new CustomMessageBox("Warning", "Do you want to install all skins at once? \nIt will reduce time to apply skin but may take ACTUALLY A LOT OF TIME and it will cost a lot of PC resourses", this) { Owner = this }.ShowDialog();

        if (result == false)
            return;

        ShowPreloader();
        _toolService.LoadChromas(1);
        HidePreloader();
        StatusLabel.Content = "Chromas installed";
    }

    private void Close(object sender, MouseButtonEventArgs e)
    {
        _preloader.Close();
        Close();
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try { DragMove(); } catch { }
        }
    }

    private void SelectGameFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Select league of legends game path",
            InitialDirectory = "C:\\",
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            _gamePath = dialog.FileName;
            if (_gamePath.Contains("egends\\Game") == false)
            {
                if (Directory.GetFiles(_gamePath).Contains("LeagueClient.exe"))
                    _gamePath = Path.Combine(_gamePath, "Game");
            }
            _toolService = new(_gamePath);
            PathSelected?.Invoke(_gamePath);
        }
    }

    private async void DownloadSkinsPreview(object sender, RoutedEventArgs e)
    {
        ShowPreloader();

        await _championService.DownloadAllPreviews();
        StatusLabel.Content = "Finished downloading champion previews";
        HidePreloader();
    }
}
