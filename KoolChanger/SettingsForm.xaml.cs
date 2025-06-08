using CSLOLTool;
using CSLOLTool.Models;
using CSLOLTool.Services;
using KoolChanger.Helpers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace KoolChanger;

public partial class SettingsWindow : Window
{
    private readonly UpdateService _updateService = new();
    private ToolService _toolService;
    private SkinService _skinService = new();
    private ChampionService _championService = new();
    private string _gamePath = string.Empty;

    public SettingsWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
            {
                BlurOpacity = 100
            };
        };
        _gamePath = LoadGamePath();
        _toolService = new(_gamePath);

        _updateService.OnUpdating += message =>
        {
            Dispatcher.Invoke(() => StatusLabel.Content = message);
        };

        _toolService.SkinInstalled += skinName =>
        {
            Dispatcher.Invoke(() => StatusLabel.Content =
                $"Installing {skinName}\n{_toolService.InstalledSkins} / {_toolService.InstallSkinCount}");
        };

        _toolService.ChromaInstalled += chromaName =>
        {
            Dispatcher.Invoke(() => StatusLabel.Content =
                $"Installing {chromaName}\n{_toolService.InstalledChromas} / {_toolService.InstallChromaCount}");
        };
    }
    public void SaveGamePath()
    {
        try
        {
            File.WriteAllText("gamepath.txt", _gamePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении пути: {ex.Message}");
        }
    }

    public string LoadGamePath()
    {
        try
        {
            if (File.Exists("gamepath.txt"))
            {
                return File.ReadAllText("gamepath.txt");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при загрузке пути: {ex.Message}");
        }

        return string.Empty;
    }
    private async void DownloadSkins_Click(object sender, RoutedEventArgs e)
    {
        await _updateService.DownloadSkins();
    }
    private async void GetChampionData(object sender, RoutedEventArgs e)
    {
        var champions = await _championService.GetChampionsAsync();
        champions.Sort((x, y) => x.Name.CompareTo(y.Name));

        foreach (var champion in champions)
        {
            champion.Skins = await _skinService.GetSkinsAsync(champion.Id);
            StatusLabel.Content = $"Getting skins for: {champion.Name}";
        }
        StatusLabel.Content = $"Finished getting info";

        File.WriteAllText("champion-data.json", JsonConvert.SerializeObject(champions));
    }

    private void InstallSkins_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => _toolService.LoadBasicSkins(30));
    }

    private void InstallChromas_Click(object sender, RoutedEventArgs e)
    {
        Task.Run(() => _toolService.LoadChromas(30));
    }

    private void CloseIcon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
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
            SaveGamePath();
            _toolService = new(_gamePath);
        }
    }
}
