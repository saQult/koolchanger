using CSLOLTool;
using CSLOLTool.Models;
using CSLOLTool.Services;
using KoolChanger.Helpers;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KoolChanger;

public partial class MainWindow : Window
{
    private readonly SkinService _skinService = new();
    private readonly ChampionService _championService = new();
    private ToolService _toolService = new("");

    private List<Champion> _champions = new();
    private Dictionary<Champion, Skin> _selectedSkins = new();
    private Process _toolProcess = new();
    private string _gamePath = string.Empty;

    private SolidColorBrush _primaryBrush = new((Color)ColorConverter.ConvertFromString("#f5dbff"));

    private const double SkinImageBaseWidth = 154;
    private const double SkinImageBaseHeight = 280;

    private Border? _selectedBorder = null;
    private Border? _selectedCircle = null;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += StartUp;

    }
    public void SaveGamePath()
    {
        try
        {
            File.WriteAllText("gamepath.txt", _gamePath);
        }
        catch (Exception ex)
        {
            statusLabel.Content  = $"Failed to save game path: {ex.Message}";
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
            statusLabel.Content = $"Failed to load game path: {ex.Message}";
        }

        return string.Empty;
    }

    private async void StartUp(object sender, RoutedEventArgs e)
    {
        DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND) { BlurOpacity = 100 };

        InitializeFoldersAndFiles();
        InitializeGamePath();
        await LoadChampionsData();
        LoadSelectedConfig();

        championListBox.ItemsSource = _champions.Select(x => x.Name);

        _toolService = new(_gamePath);
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

        if (string.IsNullOrEmpty(_gamePath) && _selectedSkins.Count > 0)
            Run();
    }
    private void InitializeFoldersAndFiles()
    {
        var folders = new[]
        {
            "installed",
            "profiles",
            "skins"
        };
        var files = new[]
        {
            "gamepath.txt",
            "selected.json",
            "champion-data.json"
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
        _gamePath = LoadGamePath();

        if (Directory.Exists(_gamePath) == false)
        {
            var path = RiotPathDetector.GetLeaguePath();
            if (string.IsNullOrEmpty(path) == false)
                _gamePath = path;
        }

        if(string.IsNullOrWhiteSpace(_gamePath))
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
                
            }
        }

        if(_gamePath.Contains("egends\\Game") == false)
        {
            if (Directory.GetFiles(_gamePath).Contains("LeagueClient.exe"))
                _gamePath = System.IO.Path.Combine(_gamePath, "Game");
        }
        SaveGamePath();
    }

    private void LoadSelectedConfig()
    {
        if (!File.Exists("selected.json"))
        {
            File.Create("selected.json").Dispose();
            return;
        }

        try
        {
            var json = File.ReadAllText("selected.json");
            var raw = JsonConvert.DeserializeObject<Dictionary<string, Skin>>(json);
            if (raw is null) return;

            _selectedSkins = raw
                .Select(pair =>
                {
                    var champ = _champions.FirstOrDefault(c => c.Name == pair.Key);
                    return champ != null ? new KeyValuePair<Champion, Skin>(champ, pair.Value) : default;
                })
                .Where(kv => kv.Key != null)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch (Exception ex)
        {
            statusLabel.Content = ex.Message;
        }
    }

    private void SaveSelectedConfig()
    {
        try
        {
            var dict = _selectedSkins.ToDictionary(kvp => kvp.Key.Name, kvp => kvp.Value);
            var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            File.WriteAllText("selected.json", json);
        }
        catch (Exception ex)
        {
            statusLabel.Content = ex.Message;
        }
    }

    private async Task LoadChampionsData()
    {
        try
        {
            if (File.Exists("champion-data.json"))
            {
                var json = File.ReadAllText("champion-data.json");
                var data = JsonConvert.DeserializeObject<List<Champion>>(json);

                if(data == null)
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

    public async Task InitializeFromServices()
    {
        statusLabel.Content = "Getting champions info";
        _champions = await _championService.GetChampionsAsync();
        _champions.Sort((x, y) => x.Name.CompareTo(y.Name));

        foreach (var champion in _champions)
        {
            champion.Skins = await _skinService.GetSkinsAsync(champion.Id);
            statusLabel.Content = $"Getting skins for: {champion.Name}";
        }

        statusLabel.Content = "Finished getting info";
        File.WriteAllText("champion-data.json", JsonConvert.SerializeObject(_champions));
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

    public Border CreateSkinBorder(string imageUrl, double width, double height, string overlayText)
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
            ((ScaleTransform)imageBrush.Transform).BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            ((ScaleTransform)imageBrush.Transform).BeginAnimation(ScaleTransform.ScaleYProperty, zoom);
            textBackground.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)));
        };

        border.MouseLeave += (s, e) =>
        {
            var zoom = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = new QuadraticEase() };
            ((ScaleTransform)imageBrush.Transform).BeginAnimation(ScaleTransform.ScaleXProperty, zoom);
            ((ScaleTransform)imageBrush.Transform).BeginAnimation(ScaleTransform.ScaleYProperty, zoom);
            textBackground.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(200)));
        };

        return border;
    }

    public void OnChampionSelected(object sender, SelectionChangedEventArgs e)
    {
        ImagePanel.Children.Clear();
        var selected = _champions.FirstOrDefault(x => x.Name == championListBox.SelectedItem?.ToString());
        if (selected == null) return;

        foreach (var skin in selected.Skins.Skip(1))
        {
            var skinPanel = new Grid
            {
                Margin = new Thickness(5),
                Width = SkinImageBaseWidth,
                Height = SkinImageBaseHeight
            };

            var skinBorder = CreateSkinBorder(skin.ImageUrl, SkinImageBaseWidth, SkinImageBaseHeight, skin.Name);

            if (_selectedSkins.TryGetValue(selected, out var s) && s.Id == skin.Id)
                SelectBorder(skinBorder, null);

            skinBorder.MouseDown += SelectBorder;
            skinBorder.MouseDown += (s, _) =>
            {
                _selectedSkins[selected] = skin;
                SaveSelectedConfig();
                Run();
            };

            Grid.SetRow(skinBorder, 1);
            skinPanel.Children.Add(skinBorder);

            if (skin.Chromas.Count > 0)
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

                    var image = new Image
                    {
                        Source = new BitmapImage(new Uri(chroma.ImageUrl)),
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

                    skinPanel.Children.Add(chromaPreviewBorder);
                    Grid.SetRow(chromaPreviewBorder, 1);
                    chromasPanel.Children.Add(circleBorder);

                    circleBorder.MouseDown += SelectCircle;
                    circleBorder.MouseDown += (s, _) =>
                    {
                        _selectedSkins[selected] = chroma;
                        SaveSelectedConfig();
                        Run();
                    };

                    circleBorder.MouseEnter += (s, _) => chromaPreviewBorder.Visibility = Visibility.Visible;
                    circleBorder.MouseLeave += (s, _) => chromaPreviewBorder.Visibility = Visibility.Collapsed;
                }

                Grid.SetRow(chromasPanel, 1);
                skinPanel.Children.Add(chromasPanelContainer);
            }

            ImagePanel.Children.Add(skinPanel);
        }
    }
    private void Run()
    {
        var selected = _selectedSkins.Values.Select(s => s is Chroma ? $"{s.Name} {s.Id}" : s.Name).ToList();

        Task.Run(() =>
        {
            try
            {
                _toolProcess.Kill();
            }
            catch { }
            _toolProcess = _toolService.Run(selected);         
        });
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
        _toolProcess.Close();
        Close();
    }
    private void Minimize(object sender, MouseButtonEventArgs e) => WindowState = WindowState.Minimized;

    private void Search(object sender, TextChangedEventArgs e)
    {
        var querry = searchTextBox.Text.ToLower();
        championListBox.ItemsSource = _champions.Where(x => x.Name.ToLower().Contains(querry)).Select(x => x.Name);
    }

    private async void OpenSettings(object sender, MouseButtonEventArgs e)
    {
        new SettingsWindow{ Owner = this }.ShowDialog();
        
        _toolService = new(LoadGamePath());
        await LoadChampionsData();
    }
}
