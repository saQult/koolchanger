#region

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Helpers;
using KoolChanger.ClientMvvm.Services;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Helpers;
using KoolChanger.Models;
using KoolChanger.Services;
using Newtonsoft.Json;

#endregion

namespace KoolChanger.ClientMvvm.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ChampionService _championService;

    private readonly INavigationService _navigationService;

    // --- Services ---
    private readonly SkinService _skinService;
    private readonly UpdateService _updateService;
    private List<Champion> _allChampions = new();

    private string _busyText = "Initializing...";

    // Sidebar List
    private ObservableCollection<ChampionListItem> _championListItems = new();
    private CustomSkinService? _customSkinService;

    private string _debugText = "";

    // Main Content
    private ObservableCollection<SkinViewModel> _displayedSkins = new();

    // --- Properties ---
    private bool _isBusy;
    private bool _isPartyModeEnabled;
    private string _lobbyId = "";
    private string _lobbyStatus = "";
    private string _members = "";
    
    private PartyService? _partyService;
    
    private Dictionary<Champion, Skin> _savedSelectedSkins = new();

    private string _searchText = "";
    private ChampionListItem? _selectedChampionItem;
    private Dictionary<Champion, Skin> _selectedSkins = new();

    private string _statusText = "Initializing...";
    private Process _toolProcess = new();
    private ToolService? _toolService;

    public MainViewModel(SkinService skinService, ChampionService championService, UpdateService updateService,
        INavigationService navigationService)
    {
        _skinService = skinService;
        _championService = championService;
        _updateService = updateService;
        _navigationService = navigationService;

        PreloaderViewModel = new PreloaderViewModel();
        
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenCustomSkinsCommand = new RelayCommand(OpenCustomSkins);
        TogglePartyModeCommand = new RelayCommand(TogglePartyMode);
        SelectSkinCommand = new RelayCommand<SkinViewModel>(SelectSkin!);
        LoadedCommand = new RelayCommand(InitializationTask);
        TogglePreloaderCommand = new RelayCommand<bool>(TogglePreloader);
    }

    // --- ViewModels ---
    public PreloaderViewModel PreloaderViewModel { get; }

    // --- State ---
    public Config Config { get; private set; } = new();

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string BusyText
    {
        get => _busyText;
        set
        {
            SetProperty(ref _busyText, value);
            PreloaderViewModel.Status = value; // Update PreloaderViewModel status
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string LobbyId
    {
        get => _lobbyId;
        set => SetProperty(ref _lobbyId, value);
    }

    public string LobbyStatus
    {
        get => _lobbyStatus;
        set => SetProperty(ref _lobbyStatus, value);
    }

    public string Members
    {
        get => _members;
        set => SetProperty(ref _members, value);
    }

    public bool IsPartyModeEnabled
    {
        get => _isPartyModeEnabled;
        set => SetProperty(ref _isPartyModeEnabled, value);
    }

    public string DebugText
    {
        get => _debugText;
        set => SetProperty(ref _debugText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterChampions();
        }
    }

    public ObservableCollection<ChampionListItem> ChampionListItems
    {
        get => _championListItems;
        set => SetProperty(ref _championListItems, value);
    }

    public ChampionListItem? SelectedChampionItem
    {
        get => _selectedChampionItem;
        set
        {
            if (SetProperty(ref _selectedChampionItem, value))
                _ = OnChampionSelectedAsync();
        }
    }

    public ObservableCollection<SkinViewModel> DisplayedSkins
    {
        get => _displayedSkins;
        set => SetProperty(ref _displayedSkins, value);
    }

    // --- Commands ---
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenCustomSkinsCommand { get; }
    public ICommand TogglePartyModeCommand { get; }
    public ICommand SelectSkinCommand { get; }
    public ICommand LoadedCommand { get; }
    public ICommand TogglePreloaderCommand { get; }

    private async void InitializationTask()
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Пиздец я просто ахуел, оказывается что подпись на событие в MainWindow.xaml.cs происходит слишком поздно и поток UI не успевает подписать, 
        // поэтому статус IsBusy вызывается в никуда, надо дождаться пока UI осилит подписаться и паттерн будет работать корректно
        await Task.Yield();
        IsBusy = true;

        await InitializeFoldersAndFiles();
        await DownloadSplashes();
        await LoadChampionsData();
        await DownloadIcons();


        if (Directory.GetDirectories("skins").Length < 170)
        {
            _updateService.OnUpdating += msg => BusyText = msg;
            await _updateService.DownloadSkins();
        }

        LoadConfig();
        InitializeGamePath();
        LoadChampionListBoxItems();
        
        
// TODO: Recode this service initialization for mvvm pattern

        _toolService = new ToolService(Config.GamePath);
        _toolService.OverlayRunned += data =>
        {
            var tooltip = data switch
            {
                "Overlay created with code: 0" => "Skins applied, waiting league match to start",
                "Overlay created with code: 1" => "Cannot apply skin, chose another or re-install skins",
                "Overlay created with code: -1" => "Something went wrong",
                _ => data
            };
            StatusText = tooltip;
            Log(data);
        };
// TODO: Recode another service initialization for mvvm pattern
        _customSkinService = new CustomSkinService(_toolService);

        StatusText = "Please, select any skin";

        if (!string.IsNullOrEmpty(Config.GamePath) && _selectedSkins.Count > 0)
            RunTool();

        PreloaderViewModel.Status = string.Empty; // Clear preloader status
        IsBusy = false;

        if (IsFirstRun())
        {
        }

        RegisterPartyService();
    }

    private void RegisterPartyService()
    {
        if (_partyService != null) return;

        _partyService = new PartyService(_allChampions, _selectedSkins, Config.PartyModeUrl);
        _partyService.OnLog += Log;
        _partyService.OnError += msg => _navigationService.ShowCustomMessageBox("Error!", msg);
        _partyService.SkinRecieved += skin =>
        {
            try
            {
                Log("Recieved skin: " + skin.Name);
                var recievedChampion = GetChampionBySkin(skin);
                if (recievedChampion != null)
                {
                    _selectedSkins[recievedChampion] = skin;
                    RefreshSelectionVisuals();
                    RunTool();
                    Log("Successfully applied skin: " + skin.Name);
                }
            }
            catch (Exception ex)
            {
                _navigationService.ShowCustomMessageBox("Error!", ex.Message);
            }
        };

        _partyService.Enabled += () => { Log("Party mode enabled"); };
        _partyService.Disabled += () =>
        {
            Log("Party mode disabled");
            LobbyId = "";
            LobbyStatus = "";
            _selectedSkins = _partyService.BackupedSkins;
        };

        _partyService.LobbyJoined += lobby =>
        {
            LobbyId = "Lobby id: " + lobby.LobbyId;
            LobbyStatus = "Lobby status: connected";
        };
        _partyService.LobbyLeaved += () =>
        {
            LobbyId = "Lobby id: none";
            LobbyStatus = "Lobby status: disconnected";
        };
    }

    private async Task OnChampionSelectedAsync()
    {
        if (SelectedChampionItem == null) return;

        DisplayedSkins.Clear();
        var selectedChamp = _allChampions.FirstOrDefault(x => x.Name == SelectedChampionItem.Name);
        if (selectedChamp == null) return;

        foreach (var skin in selectedChamp.Skins.Skip(1))
        {
            await DownloadSkinPreview(skin);

            var skinVm = new SkinViewModel
            {
                Id = skin.Id,
                Name = skin.Name,
                ImageUrl = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", skin.Id + ".png"),
                Model = skin,
                Champion = selectedChamp,
                IsSelected = IsSkinSelected(selectedChamp, skin.Id)
            };
            
            skinVm.ShowChromaPreviewCommand = new RelayCommand<SkinViewModel>(p => skinVm.ChromaPreview = p);
            skinVm.HideChromaPreviewCommand = new RelayCommand(() => skinVm.ChromaPreview = null);

            if (skin.Chromas.Count > 0)
                foreach (var chroma in skin.Chromas)
                {
                    var chromaPath = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\",
                        chroma.Id + ".png");
                    if (!File.Exists(chromaPath))
                        await _championService.DownloadImageAsync(chroma.ImageUrl, chromaPath);

                    skinVm.Children.Add(new SkinViewModel
                    {
                        Id = chroma.Id,
                        Name = chroma.Name,
                        ImageUrl = chromaPath,
                        Color = chroma.Colors.FirstOrDefault() ?? "#FFFFFF",
                        Model = chroma,
                        Champion = selectedChamp,
                        IsSelected = IsSkinSelected(selectedChamp, chroma.Id),
                        IsChroma = true
                    });
                }

            var skinIdShort = Convert.ToInt32(skin.Id.ToString().Substring(selectedChamp.Id.ToString().Length));
            var specialFormsPath = Path.Combine("skins", $"{selectedChamp.Id}", "special_forms", $"{skinIdShort}");

            if (Directory.Exists(specialFormsPath))
            {
                var sortedForms = Directory.GetFiles(specialFormsPath)
                    .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();

                foreach (var file in sortedForms)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var formImage = Path.Combine(AppContext.BaseDirectory, specialFormsPath, "models_image",
                        $"{name}.png");

                    SkinForm formSkinModel = new()
                    {
                        Id = skin.Id, Name = skin.Name, ImageUrl = skin.ImageUrl, Chromas = skin.Chromas, Stage = name
                    };

                    skinVm.Children.Add(new SkinViewModel
                    {
                        Id = skin.Id,
                        Name = name,
                        ImageUrl = formImage,
                        Model = formSkinModel,
                        Champion = selectedChamp,
                        IsSelected = IsSkinSelectedForm(selectedChamp, skin.Id, name),
                        IsForm = true
                    });
                }
            }

            DisplayedSkins.Add(skinVm);
        }
    }

    private void SelectSkin(object o)
    {
        if (o is not SkinViewModel vm) return;

        if (!IsSkinDownloaded(vm.Model))
        {
            _navigationService.ShowCustomMessageBox("Error!",
                $"This skin does not exists.\nTry to re-download skins.\nCurrent skin id - {vm.Model.Id}");
            return;
        }

        foreach (var s in DisplayedSkins)
        {
            s.IsSelected = false;
            foreach (var child in s.Children) child.IsSelected = false;
        }

        vm.IsSelected = true;

        _selectedSkins[vm.Champion] = vm.Model;
        SaveSelectedSkins();

        if (_partyService != null)
        {
            _partyService.SelectedSkins[vm.Champion] = vm.Model;
            _ = _partyService.SendSkinDataToPartyAsync(vm.Model);
        }

        RunTool();
    }

    private bool IsSkinSelected(Champion champ, long skinId)
    {
        return _selectedSkins.TryGetValue(champ, out var s) && s.Id == skinId && !(s is SkinForm);
    }

    private bool IsSkinSelectedForm(Champion champ, long skinId, string stage)
    {
        return _selectedSkins.TryGetValue(champ, out var s) && s.Id == skinId && s is SkinForm f && f.Stage == stage;
    }

    private void RunTool()
    {
        Task.Run(() =>
        {
            try
            {
                foreach (var (champion, skin) in _selectedSkins)
                {
                    var skinId = Convert.ToInt32(skin.Id.ToString()
                        .Substring(champion.Id.ToString().Length,
                            skin.Id.ToString().Length - champion.Id.ToString().Length));

                    if (skin is SkinForm skinForm)
                    {
                        var skinPath = Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinId}",
                            $"{skinForm.Stage}.fantome");
                        if (!Directory.Exists(Path.Combine("installed", $"{skin.Id}-{skinForm.Stage}")))
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }
                    else
                    {
                        var skinPath = Path.Combine("skins", $"{champion.Id}", $"{skinId}.fantome");
                        if (!Directory.Exists(Path.Combine("installed", $"{skin.Id}")))
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }
                }
                
                try
                {
                    _toolProcess.Kill();
                    _toolProcess.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"Error killing tool process: {ex.Message}");
                }

                var selected = _selectedSkins.Values.Select(x => x.Id.ToString()).ToList();
                if (_customSkinService != null)
                    selected.AddRange(_customSkinService.ImportedSkins.Where(x => x.Enabled).Select(x => x.Name));

                _toolProcess = _toolService.Run(selected.Where(x => Directory.Exists(Path.Combine("installed", x))));
            }
            catch (Exception ex)
            {
                Log("Run Error: " + ex.Message);
            }
        });
    }

    private void LoadChampionListBoxItems()
    {
        var list = new ObservableCollection<ChampionListItem>();
        foreach (var champion in _allChampions)
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "champions", $"{champion.Id}.png");
            list.Add(new ChampionListItem(iconPath, champion.Name));
        }
        ChampionListItems = list;
    }

    private void FilterChampions()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            LoadChampionListBoxItems();
            return;
        }
        var query = SearchText.ToLower();
        var filtered = _allChampions.Where(c => c.Name.ToLower().Contains(query));
        var list = new ObservableCollection<ChampionListItem>();
        foreach (var c in filtered)
            list.Add(new ChampionListItem(Path.Combine(AppContext.BaseDirectory, "assets", "champions", $"{c.Id}.png"),
                c.Name));
        ChampionListItems = list;
    }

    private void RefreshSelectionVisuals()
    {
        if (SelectedChampionItem != null)
            _ = OnChampionSelectedAsync();
    }

    private Champion? GetChampionBySkin(Skin skin)
    {
        return _allChampions.FirstOrDefault(c => c.Skins.Any(x => x.Id == skin.Id));
    }

    private bool IsSkinDownloaded(Skin skin)
    {
        var champion = GetChampionBySkin(skin);
        if (champion == null) return false;
        var skinId = Convert.ToInt32(skin.Id.ToString()
            .Substring(champion.Id.ToString().Length,
                skin.Id.ToString().Length - champion.Id.ToString().Length));

        if (skin is SkinForm skinForm)
            return File.Exists(Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinId}",
                $"{skinForm.Stage}.fantome"));

        return File.Exists(Path.Combine("skins", $"{champion.Id}", $"{skinId}.fantome"));
    }

    private void Log(string msg)
    {
        DebugText = msg + "\n" + DebugText;
    }

    private void LoadConfig()
    {
        if (File.Exists("config.json"))
        {
            var data = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            if (data != null)
            {
                Config = data;
                _selectedSkins = Config.SelectedSkins
                    .Select(pair =>
                    {
                        var champ = _allChampions.FirstOrDefault(c => c.Name == pair.Key);
                        return champ != null ? new KeyValuePair<Champion, Skin>(champ, pair.Value) : default;
                    })
                    .ToDictionary(kv => kv.Key!, kv => kv.Value);
            }
        }
    }

    public void SaveConfig()
    {
        File.WriteAllText("config.json", JsonConvert.SerializeObject(Config));
    }

    private void SaveSelectedSkins()
    {
        Config.SelectedSkins = _selectedSkins.ToDictionary(k => k.Key.Name, v => v.Value);
        SaveConfig();
    }

    private void OpenSettings()
    {
        _navigationService.ShowDialog<SettingsViewModel>();
    }

    private void OpenCustomSkins()
    {
        _navigationService.ShowDialog<CustomSkinsViewModel>();
    }

    private async void TogglePartyMode()
    {
        if (IsPartyModeEnabled)
        {
            _selectedSkins = _savedSelectedSkins;
            _savedSelectedSkins = new Dictionary<Champion, Skin>();
            if (_partyService != null) await _partyService.DisableAsync();
            LobbyId = "";
            LobbyStatus = "";
            Members = "";
        }
        else
        {
            IsBusy = true;
            if (_partyService != null) await _partyService.EnableAsync(_selectedSkins);
            _savedSelectedSkins = _selectedSkins;
            _selectedSkins = new Dictionary<Champion, Skin>();
            IsBusy = false;
        }
    }

    private async Task DownloadSplashes()
    {
        if (Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "assets", "champions", "splashes")).Length == 0)
        {
            var resultToDownloadSkinsPreview = _navigationService.ShowCustomMessageBox("Info",
                "Do you want to download preview for champion skins now? " +
                "If not, previews will download in real time when you select any champion").DialogResult;
            _championService.OnDownloaded += message => BusyText = message;
            await _championService.DownloadAllPreviews();
        }
    }

    private async Task LoadChampionsData()
    {
        try
        {
            if (File.Exists("champion-data.json"))
            {
                var data = JsonConvert.DeserializeObject<List<Champion>>(File.ReadAllText("champion-data.json"));
                _allChampions = data ?? await InitializeFromServices();
            }
            else
            {
                _allChampions = await InitializeFromServices();
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading data: {ex.Message}";
            Log($"Error loading data: {ex.Message}");
        }
    }

    private async Task<List<Champion>> InitializeFromServices()
    {
        StatusText = "Getting champions info";
        var result = await _skinService.GetAllSkinsAsync(await _championService.GetChampionsAsync());
        result.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        StatusText = "Finished getting info";
        File.WriteAllText("champion-data.json", JsonConvert.SerializeObject(result));
        return result;
    }

    private async Task DownloadIcons()
    {
        var semaphore = new SemaphoreSlim(50);
        var tasks = new List<Task>();

        foreach (var champion in _allChampions)
        {
            await semaphore.WaitAsync();
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var iconPath = Path.Combine("assets", "champions", $"{champion.Id}.png");
                    if (!File.Exists(iconPath))
                    {
                        BusyText = $"Downloading icon for {champion.Name}";
                        await _championService.DownloadChampionIconAsync(champion.Id, "assets\\champions");
                    }
                }
                catch (Exception ex)
                {
                    BusyText = $"Error downloading icon for {champion.Name}: {ex.Message}";
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        await Task.WhenAll(tasks);
    }

    private async Task DownloadSkinPreview(Skin skin)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "assets\\champions\\splashes\\", skin.Id + ".png");
        if (!File.Exists(path)) await _championService.DownloadImageAsync(skin.ImageUrl, path);
    }

    private Task InitializeFoldersAndFiles()
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
            "customskins.json",
            "config.json"
        };

        foreach (var folder in folders)
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

        foreach (var file in files)
            if (!File.Exists(file))
                File.Create(file).Dispose();
        return Task.CompletedTask;
    }

    private void InitializeGamePath()
    {
        if (!Directory.Exists(Config.GamePath))
        {
            var path = RiotPathDetector.GetLeaguePath();
            if (!string.IsNullOrEmpty(path)) Config.GamePath = path;
        }

        SaveConfig();
    }

    private bool IsFirstRun()
    {
        if (!File.Exists("runned"))
        {
            File.Create("runned").Dispose();
            return true;
        }

        return false;
    }

    private void TogglePreloader(bool show)
    {
       
    }
}

public class SkinViewModel : ObservableObject
{
    private bool _isSelected;
    private SkinViewModel? _chromaPreview;
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public bool IsChroma { get; set; }
    public bool IsForm { get; set; }

    public Skin Model { get; set; } = null!; 
    public Champion Champion { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public SkinViewModel? ChromaPreview
    {
        get => _chromaPreview;
        set => SetProperty(ref _chromaPreview, value);
    }

    public ICommand? ShowChromaPreviewCommand { get; set; }
    public ICommand? HideChromaPreviewCommand { get; set; }

    public ObservableCollection<SkinViewModel> Children { get; set; } = new();
}

public class ChampionListItem(string u, string n)
{
    public string IconUrl { get; set; } = u;
    public string Name { get; set; } = n;
}