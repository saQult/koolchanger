﻿using CSLOLTool.Dto;
using CSLOLTool.Models;
using CSLOLTool.Services;
using KoolChanger.Helpers;
using LCUSharp;
using LCUSharp.Websocket;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace KoolChanger;

public partial class MainWindow : Window
{
    private Config _config = new();

    private readonly SkinService _skinService = new();
    private readonly ChampionService _championService = new();
    private readonly UpdateService _updateService = new();
    private ToolService _toolService = new("");
    private LobbyService _lobbyService = new();
    private LCUService _lcuService = new();

    private List<Champion> _champions = new();
    private List<ChampionListItem> _championsList = new();
    private Dictionary<Champion, Skin> _selectedSkins = new();
    private Dictionary<Champion, Skin> _savedSelectedSkins = new();
    private LobbyData _currentLobby;

    private Process _toolProcess = new();
    private HubConnection _lobbyConnection;

    private SolidColorBrush _primaryBrush = new((Color)ColorConverter.ConvertFromString("#f5dbff"));

    private const double SkinImageBaseWidth = 154;
    private const double SkinImageBaseHeight = 280;

    private Border? _selectedBorder = null;
    private Border? _selectedCircle = null;
    
    private Preloader _preloader = new();

    public MainWindow()
    {
        InitializeComponent();

        _preloader = new() {WindowStartupLocation = WindowStartupLocation.CenterScreen};
        _preloader.Topmost = true;

        Loaded += StartUp;
        Closed += (_, _) => 
        {
            _preloader.Close();
            KillToolProcess();
        };

    }

    #region Configuration
    private async void StartUp(object sender, RoutedEventArgs e)
    {
        DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND) { BlurOpacity = 100 };

        InitializeFoldersAndFiles();

        await DownloadSplashes();

        await LoadChampionsData();
        await DownloadIcons();

        if (Directory.GetDirectories("skins").Length < 170)
        {
            _updateService.OnUpdating += (data) => _preloader.SetStatus(data);
            await _updateService.DownloadSkins();
        }

        LoadConfig();
        InitializeGamePath();
        LoadChampionListBoxItems();

        _toolService = new(_config.GamePath);
        string tooltip = "";
        _toolService.OverlayRunned += data =>
        {
            tooltip = data switch
            {
                "Overlay created with code: 0" => "Skins applied, waiting league match to start",
                "Overlay created with code: 1" => "Cannot apply skin, chose another or re-install skins",
                "Overlay created with code: -1" => "Something went wrong",
                _ => data
            };
            Dispatcher.Invoke(() => statusLabel.Content = tooltip);
        };

        statusLabel.Content = "Please, select any skin";

        KillToolProcess();
        if (string.IsNullOrEmpty(_config.GamePath) == false && _selectedSkins.Count > 0)
            Run();

        HidePreloader();
        _preloader = new() { Owner = this };

        if(IsFirstRun())
        {
            Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }
    }
    private bool IsFirstRun()
    {
        string firstRunMarkerPath = Path.Combine(AppContext.BaseDirectory, "runned");

        if (!File.Exists(firstRunMarkerPath))
        {
            File.Create(firstRunMarkerPath).Dispose();
            return true;
        }
        else
        {
            return false;
        }
    }
    private async Task DownloadSplashes()
    {
        bool? resultToDownloadSkinsPreview = false;

        if (Directory.GetFiles("assets\\champions\\splashes").Length == 0)
            resultToDownloadSkinsPreview = new CustomMessageBox("Info", "Do you want to download preview for champion skins now? " +
                "If not, previews will download in real time when you select any champion", this).ShowDialog();

        ShowPreloader();

        if (resultToDownloadSkinsPreview == true)
        {
            _championService.OnDownloaded += (message) => _preloader.SetStatus(message);
            await _championService.DownloadAllPreviews();
        }
    }
    private void LoadChampionListBoxItems()
    {
        foreach (var champion in _champions)
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "champions", $"{champion.Id}.png");
            _championsList.Add(new ChampionListItem(iconPath, champion.Name));
        }
        championListBox.ItemsSource = _championsList;
    }
    public void LoadConfig()
    {
        if (File.Exists("config.json"))
        {
            var data = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            if (data != null)
            {
                _config = data;

                _selectedSkins = _config.SelectedSkins
                    .Select(pair =>
                    {
                        var champ = _champions.FirstOrDefault(c => c.Name == pair.Key);
                        return champ != null ? new KeyValuePair<Champion, Skin>(champ, pair.Value) : default;
                    })
                    .Where(kv => kv.Key != null)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }
    }
    public void SaveConfig()
    {
        File.WriteAllText("config.json", JsonConvert.SerializeObject(_config));
    }
    private void InitializeFoldersAndFiles()
    {
        var folders = new[]
        {
            "installed",
            "profiles",
            "skins",
            "assets\\champions",
            "assets\\champions\\splashes"
        };
        var files = new[]
        {
            "champion-data.json",
            "config.json"
        };

        foreach (var folder in folders)
            if (Directory.Exists(folder) == false)
                Directory.CreateDirectory(folder);

        foreach (var file in files)
            if (File.Exists(file) == false)
                File.Create(file).Dispose();
    }
    private void InitializeGamePath()
    {
        if (Directory.Exists(_config.GamePath) == false)
        {
            var path = RiotPathDetector.GetLeaguePath();
            if (string.IsNullOrEmpty(path) == false)
                _config.GamePath = path;
        }

        if (string.IsNullOrWhiteSpace(_config.GamePath))
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select league of legends game path",
                InitialDirectory = "C:\\",
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _config.GamePath = dialog.FileName;

            }
        }

        if (_config.GamePath.Contains("egends\\Game") == false)
        {
            if (Directory.GetFiles(_config.GamePath).Contains("LeagueClient.exe"))
                _config.GamePath = Path.Combine(_config.GamePath, "Game");
        }
        SaveConfig();
    }
    private void SaveSelectedSkins()
    {
        try
        {
            _config.SelectedSkins = _selectedSkins.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
            SaveConfig();
        }
        catch (Exception ex)
        {
            new CustomMessageBox("Error!", ex.Message, this);
        }

    }
    private void KillToolProcess()
    {
        try
        {
            var processes = Process.GetProcessesByName("cslolmoodtool");
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }
        catch { }
    }
    #endregion
    #region Data
    private async Task DownloadIcons()
    {
        var semaphore = new SemaphoreSlim(50);
        var tasks = new List<Task>();

        foreach (var champion in _champions)
        {
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var iconPath = Path.Combine("assets", "champions", $"{champion.Id}.png");
                    if (!File.Exists(iconPath))
                    {
                        _preloader.SetStatus($"Downloading icon for {champion.Name}");
                        await _championService.DownloadChampionIconAsync(champion.Id, "assets\\champions");
                    }
                }
                catch (Exception ex)
                {
                    _preloader.SetStatus($"Error downloading icon: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
    private async Task InitializeFromServices()
    {
        statusLabel.Content = "Getting champions info";
        _champions = await _skinService.GetAllSkinsAsync(await _championService.GetChampionsAsync());
        _champions.Sort((x, y) => x.Name.CompareTo(y.Name));
        statusLabel.Content = "Finished getting info";
        File.WriteAllText("champion-data.json", JsonConvert.SerializeObject(_champions));
    }
    private async Task LoadChampionsData()
    {
        try
        {
            if (File.Exists("champion-data.json"))
            {
                var json = File.ReadAllText("champion-data.json");
                var data = JsonConvert.DeserializeObject<List<Champion>>(json);

                if (data == null)
                    await InitializeFromServices();
                else
                    _champions = data;
            }
            else
            {
                await InitializeFromServices();
            }
        }
        catch
        {
            statusLabel.Content = $"Error loading data, try to update in from settings";
        }
    }
    private void Search(object sender, TextChangedEventArgs e)
    {
        var querry = searchTextBox.Text.ToLower();
        championListBox.ItemsSource = _championsList.Where(x => x.Name.ToLower().Contains(querry));
    }

    #endregion
    #region UI
    private void ShowPreloader()
    {
        Effect = new BlurEffect { Radius = 10 };
        _preloader.Show();
    }
    private void HidePreloader()
    {
        Effect = null;
        _preloader.Hide();
    }
    private void ResetSelection()
    {
        if (_selectedBorder != null)
            _selectedBorder.BorderBrush = _primaryBrush;

        if (_selectedCircle != null)
            _selectedCircle.BorderBrush = Brushes.Transparent;
    }
    private void SelectBorder(object sender, MouseButtonEventArgs? e)
    {
        if (sender is not Border clickedBorder)
            return;

        ResetSelection();
        clickedBorder.BorderBrush = Brushes.Lime;
        _selectedBorder = clickedBorder;
        _selectedCircle = null;
    }
    private void SelectCircle(object sender, MouseButtonEventArgs? e)
    {
        if (sender is not Border clickedCircleBorder)
            return;

        DependencyObject skinPanel = VisualTreeHelper.GetParent(clickedCircleBorder);
        while (skinPanel != null && skinPanel is not Grid)
            skinPanel = VisualTreeHelper.GetParent(skinPanel);

        var grid = skinPanel as Grid;
        var skinBorder = grid?.Children.OfType<Border>().FirstOrDefault(b => b.Tag?.ToString() == "SkinBorder");

        ResetSelection();
        clickedCircleBorder.BorderBrush = Brushes.White;
        _selectedCircle = clickedCircleBorder;

        if (skinBorder != null)
        {
            skinBorder.BorderBrush = Brushes.Lime;
            _selectedBorder = skinBorder;
        }
    }
    private Border CreateSkinBorder(string imageUrl, double width, double height, string overlayText)
    {
        var imageBrush = new ImageBrush(new BitmapImage(new Uri(imageUrl)))
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center,
            Transform = new ScaleTransform(1.0, 1.0, 0.5, 0.5)
        };

        var border = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            BorderBrush = _primaryBrush,
            Background = imageBrush,
            ClipToBounds = true,
            Cursor = Cursors.Hand,
            Tag = "SkinBorder"
        };

        var grid = new Grid();
        var textBackground = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Height = 30,
            Opacity = 0,
            CornerRadius = new CornerRadius(10, 10, 0, 0)
        };

        var overlayTextBlock = new TextBlock
        {
            Text = overlayText,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(5)
        };

        textBackground.Child = overlayTextBlock;
        grid.Children.Add(textBackground);
        border.Child = grid;

        border.MouseEnter += (s, e) =>
        {
            var zoom = new DoubleAnimation(1.1, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase() };
            ((ScaleTransform)border.Background.Transform).BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            ((ScaleTransform)border.Background.Transform).BeginAnimation(ScaleTransform.ScaleYProperty, zoom);
            textBackground.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)));
        };

        border.MouseLeave += (s, e) =>
        {
            var zoom = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase() };
            ((ScaleTransform)border.Background.Transform).BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            ((ScaleTransform)border.Background.Transform).BeginAnimation(ScaleTransform.ScaleYProperty, zoom);
            textBackground.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(200)));
        };

        return border;
    }
    private async void OnChampionSelected(object sender, SelectionChangedEventArgs e)
    {
        if (championListBox.SelectedItem is null)
            return;

        ImagePanel.Children.Clear();

        var selected = _champions.FirstOrDefault(x => x.Name == (championListBox.SelectedItem as ChampionListItem)!.Name);
        if (selected == null) return;

        foreach (var skin in selected.Skins.Skip(1))
        {
            var skinPanel = CreateSkinPanel();

            await DownloadSkinPreview(skin);

            var skinBorder = CreateSkinBorder(Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", skin.Id + ".png"), 
                SkinImageBaseWidth, 
                SkinImageBaseHeight, 
                skin.Name);

            if (_selectedSkins.TryGetValue(selected, out var s) && s.Id == skin.Id)
                SelectBorder(skinBorder, null);

            skinBorder.MouseDown += SelectBorder;
            skinBorder.MouseDown += async (s, _) =>
            {
                _selectedSkins[selected] = skin;
                if (_lobbyConnection != null)
                {
                    var data = _selectedSkins.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
                    try
                    {
                        await _lobbyConnection.InvokeAsync("SendMessage", _currentLobby.LobbyId,
                            JsonConvert.SerializeObject(data));
                    } catch(Exception ex)
                    {
                        new CustomMessageBox("Error!", "Error applying skin: " + ex.Message, this).ShowDialog();
                    }

                }
                SaveSelectedSkins();
                Run();
            };

            Grid.SetRow(skinBorder, 1);
            skinPanel.Children.Add(skinBorder);

            if (skin.Chromas.Count > 0)
                await AddChromasAsync(skin, selected, skinPanel);

            AddSpecialForms(skin, selected, skinPanel);

            ImagePanel.Children.Add(skinPanel);
        }
    }
    private Grid CreateSkinPanel()
    {
        return new Grid
        {
            Margin = new Thickness(5),
            Width = SkinImageBaseWidth,
            Height = SkinImageBaseHeight
        };
    }
    private async Task DownloadSkinPreview(Skin skin)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", skin.Id + ".png");
        if (!File.Exists(path))
            await _championService.DownloadImageAsync(skin.ImageUrl, path);
    }
    private async Task AddChromasAsync(Skin skin, Champion selected, Grid skinPanel)
    {
        var chromasPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(5)
        };

        var chromasPanelContainer = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30)),
            Child = chromasPanel,
            Margin = new Thickness(10),
            VerticalAlignment = VerticalAlignment.Bottom
        };

        foreach (var chroma in skin.Chromas)
        {
            var color = (Color)ColorConverter.ConvertFromString(chroma.Colors.FirstOrDefault() ?? "#FFFFFF");

            var skinImagePath = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", chroma.Id + ".png");
            if (!File.Exists(skinImagePath))
                await _championService.DownloadImageAsync(chroma.ImageUrl, skinImagePath);

            var image = new Image
            {
                Source = new BitmapImage(new Uri(skinImagePath)),
                Width = 130,
                Height = 200,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };

            var chromaPreviewBorder = new Border
            {
                Width = 130,
                Height = 160,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 13, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = image
            };

            var ellipse = new Ellipse
            {
                Width = 14,
                Height = 14,
                Fill = new SolidColorBrush(color),
                Cursor = Cursors.Hand,
                Tag = chroma
            };

            var circleBorder = new Border
            {
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(3),
                Child = ellipse,
                Tag = ellipse
            };

            if (_selectedSkins.TryGetValue(selected, out var cs) && cs.Id == chroma.Id)
                SelectCircle(circleBorder, null);

            circleBorder.MouseDown += SelectCircle;
            circleBorder.MouseDown += async (s, _) =>
            {
                _selectedSkins[selected] = chroma;
                if (_lobbyConnection != null)
                {
                    var data = _selectedSkins.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
                    try
                    {
                        await _lobbyConnection.InvokeAsync("SendMessage", _currentLobby.LobbyId,
                            JsonConvert.SerializeObject(data));
                    }
                    catch (Exception ex)
                    {
                        new CustomMessageBox("Error!", "Error applying skin: " + ex.Message, this).ShowDialog();
                    }
                }
                else
                    SaveSelectedSkins();
                Run();
            };

            circleBorder.MouseEnter += (s, _) => chromaPreviewBorder.Visibility = Visibility.Visible;
            circleBorder.MouseLeave += (s, _) => chromaPreviewBorder.Visibility = Visibility.Collapsed;

            skinPanel.Children.Add(chromaPreviewBorder);
            Grid.SetRow(chromaPreviewBorder, 1);
            chromasPanel.Children.Add(circleBorder);
        }

        Grid.SetRow(chromasPanel, 1);
        skinPanel.Children.Add(chromasPanelContainer);
    }
    private void AddSpecialForms(Skin skin, Champion selected, Grid skinPanel)
    {
        var skinId = Convert.ToInt32(skin.Id.ToString().Substring(selected.Id.ToString().Length));
        var specialFormsPath = Path.Combine("skins", $"{selected.Id}", "special_forms", $"{skinId}");

        if (!Directory.Exists(specialFormsPath)) return;

        var formsPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(5)
        };

        var formsPanelContainer = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(150, 30, 30, 30)),
            Child = formsPanel,
            Margin = new Thickness(10),
            VerticalAlignment = VerticalAlignment.Bottom
        };

        var sortedFormsFileList = Directory.GetFiles(specialFormsPath)
            .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
            .ToList();

        foreach (var file in sortedFormsFileList)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var formImage = Path.Combine(AppContext.BaseDirectory, specialFormsPath, "models_image", $"{name}.png");

            var image = new Image
            {
                Source = new BitmapImage(new Uri(formImage)),
                Width = 130,
                Height = 200,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };

            var formPreviewBorder = new Border
            {
                Width = 130,
                Height = 210,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 13, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = image
            };

            var formGrid = CreateFormGrid(name);

            var circleBorder = new Border
            {
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(3),
                Child = formGrid,
                Tag = formGrid
            };

            circleBorder.MouseDown += SelectCircle;
            circleBorder.MouseDown += async (s, _) =>
            {
                SkinForm formSkin = new()
                {
                    Id = skin.Id,
                    Name = skin.Name,
                    ImageUrl = skin.ImageUrl,
                    Chromas = skin.Chromas,
                    Stage = name
                };

                _selectedSkins[selected] = formSkin;
                if (_lobbyConnection != null)
                {
                    var data = _selectedSkins.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
                    try
                    {
                        await _lobbyConnection.InvokeAsync("SendMessage", _currentLobby.LobbyId,
                            JsonConvert.SerializeObject(data));
                    }
                    catch (Exception ex)
                    {
                        new CustomMessageBox("Error!", "Error applying skin: " + ex.Message, this).ShowDialog();
                    }
                }
                else
                    SaveSelectedSkins();
                Run();
            };

            circleBorder.MouseEnter += (s, _) => formPreviewBorder.Visibility = Visibility.Visible;
            circleBorder.MouseLeave += (s, _) => formPreviewBorder.Visibility = Visibility.Collapsed;

            formsPanel.Children.Add(circleBorder);

            skinPanel.Children.Add(formPreviewBorder);
            Grid.SetRow(formPreviewBorder, 1);
        }

        Grid.SetRow(formsPanel, 1);
        skinPanel.Children.Add(formsPanelContainer);
    }
    private Grid CreateFormGrid(string name)
    {
        var formText = new TextBlock
        {
            Text = name,
            Foreground = _primaryBrush,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var formGrid = new Grid
        {
            Width = 22,
            Height = 22,
            Cursor = Cursors.Hand,
        };

        formGrid.Children.Add(new Ellipse
        {
            Width = 22,
            Height = 22,
            Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
        });

        formGrid.Children.Add(formText);
        return formGrid;
    }
    private void DragMove(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }
        catch { }
    }
    private void CloseApp(object sender, MouseButtonEventArgs e)
    {
        _preloader.Close();
        _toolProcess.Close();
        Application.Current.Shutdown();
    }
    private void Minimize(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;
    private async void OpenSettings(object sender, MouseButtonEventArgs e)
    {
        Effect = new BlurEffect { Radius = 10 };
        var window = new SettingsWindow(_config.GamePath) { Owner = this };
        window.PathSelected += (path) =>
        {
            _config.GamePath = path;
            SaveConfig();
        };
        window.ShowDialog();
        LoadConfig();
        _toolService = new(_config.GamePath);
        await LoadChampionsData();
        Effect = null;
    }
    #endregion
    #region Party mode
    private async void EnablePartyMode(object sender, RoutedEventArgs e)
    {
        partyModeCheckbox.IsEnabled = false;
        ShowPreloader();
        
        BackupSelectedSkins();

        await _lcuService.ConnectAsync();
        try
        {
            _currentLobby = await _lobbyService.ExtractLobbyInfoAsync();
            await ConnectToLobby(_currentLobby);
        }
        catch { }
        _lcuService.GameFlowChanged += OnGameFlowChanged;
        _lcuService.SubscrbeLobbyEvent();

        HidePreloader();
        partyModeCheckbox.IsEnabled = true;
    }
    private async void DisablePartyMode(object sender, RoutedEventArgs e)
    {
        RestoreSelectedSkins();
        await _lobbyConnection.DisposeAsync();
        _lcuService.Api.Disconnect();
        Dispatcher.Invoke(() => lobbyStatusLabel.Content = "Lobby status: disconnected");
    }
    private void BackupSelectedSkins()
    {
        if (_savedSelectedSkins.Count > 0)
            _savedSelectedSkins = _selectedSkins;
        _selectedSkins = new();
    }
    private void RestoreSelectedSkins()
    {
        _selectedSkins = _savedSelectedSkins;
        _savedSelectedSkins = new();
    }
    private async void OnGameFlowChanged(object? sender, LeagueEvent e)
    {
        var data = e.Data.ToString();

        if (data == "Lobby")
        {
            _currentLobby = await _lobbyService.ExtractLobbyInfoAsync();
            await ConnectToLobby(_currentLobby);
        }
    }
    private async Task ConnectToLobby(LobbyData lobby)
    {
        try
        {
            _lobbyConnection = _lobbyService.CreateConnection();
            RegisterLobbyHandlers();

            await _lobbyConnection.StartAsync();
            await JoinOrCreateLobby(lobby);
            Dispatcher.Invoke(() => lobbyStatusLabel.Content = "Lobby status: connected");
        }
        catch  
        {
            Dispatcher.Invoke(() => new CustomMessageBox("Error!", "Failed to connect to looby, try again", this).ShowDialog());
        }

    }
    private void RegisterLobbyHandlers()
    {
        _lobbyConnection.On<LobbyMember>("MemberJoined", async member =>
        {
            var data = _selectedSkins.ToDictionary(kvp => kvp.Key.Id, kvp => kvp.Value);
            try
            {
                await _lobbyConnection.InvokeAsync("SendMessage", _currentLobby.LobbyId,
                    JsonConvert.SerializeObject(data));
            }
            catch (Exception ex)
            {
                new CustomMessageBox("Error!", "Error applying skin: " + ex.Message, this).ShowDialog();
            }
        });

        _lobbyConnection.On<string, string, string>("ReceiveMessage", (lobbyId, puuid, msg) =>
        {
            if (puuid == _currentLobby.LocalMember.Puuid)
                return;
            Dispatcher.Invoke(() => { });

            var data = JsonConvert.DeserializeObject<Dictionary<int, Skin>>(msg);
            if (data == null)
                return;

            var skins = data.ToDictionary(
                kvp => _champions.First(c => c.Id == kvp.Key),
                kvp => kvp.Value
            );

            if (skins == null)
                return;

            foreach (var pair in skins)
            {
                _selectedSkins[pair.Key] = pair.Value;
            }

            Run();
        });
    }
    private async Task JoinOrCreateLobby(LobbyData lobby)
    {
        bool lobbyFound = false;

        foreach (var member in lobby.Members)
        {
            var result = await _lobbyConnection.InvokeAsync<bool>("JoinLobby", member.Puuid, lobby.LocalMember.Puuid);
            if (result)
            {
                lobbyFound = true;
                _currentLobby.LobbyId = member.Puuid;
                break;
            }
        }

        if (!lobbyFound)
        {
            await _lobbyConnection.InvokeAsync("CreateLobby", lobby.LocalMember.Puuid, lobby.LocalMember.Puuid);
            Dispatcher.Invoke(() => lobbyStatusLabel.Content = "Lobby status: connected");
            _currentLobby.LobbyId = lobby.LocalMember.Puuid;
        }
    }
    #endregion
    private void Run()
    {
        Task.Run(() =>
        {
            try
            {
                foreach (var kvp in _selectedSkins)
                {
                    var skin = kvp.Value;
                    var champion = kvp.Key;
                    
                    var skinId = Convert.ToInt32(skin.Id.ToString()
                        .Substring(champion.Id.ToString().Length,
                        skin.Id.ToString().Length - champion.Id.ToString().Length));


                    if (skin is SkinForm skinForm)
                    {
                       var skinPath = Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinId}", $"{skinForm.Stage}.fantome");
                       if (Directory.Exists(Path.Combine("installed", $"{skin.Id}-{skinForm.Stage}")) == false)
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }
                    else
                    {
                        var skinPath = Path.Combine("skins", $"{champion.Id}", $"{skinId}.fantome");

                        if (Directory.Exists(Path.Combine("installed", $"{skin.Id}")) == false)
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }


                }
                if (_toolProcess != null)
                {
                    _toolProcess.Kill();
                }

            }
            catch {}
            _toolProcess = _toolService.Run(_selectedSkins.Values.Select(x => x.Id.ToString()).ToList());
        });
    }

}
public class ChampionListItem(string iconUrl, string name)
{
    public string IconUrl { get; set; } = iconUrl;
    public string Name { get; set; } = name;
}