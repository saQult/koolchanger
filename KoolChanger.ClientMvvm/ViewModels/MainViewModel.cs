using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Helpers;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Models;
using KoolChanger.Services;


namespace KoolChanger.ClientMvvm.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IConfigService _configService;
    private readonly IFilesystemService _filesystemService;
    private readonly ILoggingService _loggingService;
    private readonly IDataInitializationService _dataInitService;

    private readonly ChampionService _championService;
    private readonly UpdateService _updateService;
    private readonly KoolService _koolService;

    private ToolService? _toolService;
    private PartyService? _partyService;
    private CustomSkinService? _customSkinService;

    private List<Champion> _allChampions = new();
    private Dictionary<Champion, Skin> _selectedSkins = new();
    private Dictionary<Champion, Skin> _savedSelectedSkins = new(); // Для Party Mode
    private Config Config { get; set; } = new();

    private ObservableCollection<ChampionListItem> _championListItems = new();
    private ObservableCollection<SkinViewModel> _displayedSkins = new();
    private Dictionary<Champion, ObservableCollection<SkinViewModel>> _allSkins = new();

    private string _busyText = "Initializing...";
    private string _debugText = "";
    private bool _isBusy;
    private bool _isPartyModeEnabled;
    private string _lobbyId = "";
    private string _lobbyStatus = "";
    private string _statusText = "Initializing...";
    private string _searchText = "";
    private ChampionListItem? _selectedChampionItem;
    private string _members = "";

    public MainViewModel(
        ChampionService championService,
        UpdateService updateService,
        KoolService koolService,
        INavigationService navigationService,
        IConfigService configService,
        IFilesystemService filesystemService,
        ILoggingService loggingService,
        IDataInitializationService dataInitService)
    {
        _championService = championService;
        _updateService = updateService;
        _navigationService = navigationService;
        _koolService = koolService;
        _configService = configService;
        _filesystemService = filesystemService;
        _loggingService = loggingService;
        _dataInitService = dataInitService;

        PreloaderViewModel = new PreloaderViewModel();

        _loggingService.OnLog += LogToDebugText;
        _dataInitService.OnUpdating += data => BusyText = data;
        _updateService.OnUpdating += message => StatusText  = message;
        _updateService.OnUpdating += LogToDebugText;
        
        
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenCustomSkinsCommand = new RelayCommand(OpenCustomSkins);
        TogglePartyModeCommand = new AsyncRelayCommand(TogglePartyModeAsync);
        SelectSkinCommand = new RelayCommand<SkinViewModel>(SelectSkin);
        LoadedCommand = new AsyncRelayCommand(InitializeAsync);
        TogglePreloaderCommand = new RelayCommand<bool>(TogglePreloader);
    }

    public PreloaderViewModel PreloaderViewModel { get; }

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
            if (SetProperty(ref _busyText, value))
            {
                PreloaderViewModel.Status = value;
            }
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
            {
                _ = OnChampionSelectedAsync();
            }
        }
    }

    public ObservableCollection<SkinViewModel> DisplayedSkins
    {
        get => _displayedSkins;
        set => SetProperty(ref _displayedSkins, value);
    }

    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenCustomSkinsCommand { get; }
    public IAsyncRelayCommand TogglePartyModeCommand { get; }
    public ICommand SelectSkinCommand { get; }
    public IAsyncRelayCommand LoadedCommand { get; }
    public ICommand TogglePreloaderCommand { get; }

    private async Task InitializeAsync()
    {
        await Task.Yield();
        IsBusy = true;

        BusyText = "Initializing folders and files...";
        await _filesystemService.InitializeFoldersAndFilesAsync();

        Config = _configService.LoadConfig();

        BusyText = "Loading champion data and assets...";
        await _dataInitService.InitializeDataAsync(Config);
        _allChampions = _dataInitService.AllChampions;

        _selectedSkins = _configService.LoadSelectedSkins(_allChampions);

        _configService.InitializeGamePath(Config);
        _configService.SaveConfig(Config);

        LoadChampionListBoxItems();

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
            _loggingService.Log(data);
        };

        _customSkinService = new CustomSkinService(_toolService);

        StatusText = "Please, select any skin";

        if (!string.IsNullOrEmpty(Config.GamePath) && _selectedSkins.Count > 0)
            RunTool();

        PreloaderViewModel.Status = string.Empty;
        IsBusy = false;

        if (_filesystemService.IsFirstRun())
        {
            // implement pls
        }

        RegisterPartyService();
        LoadChampionListBoxItems();

        foreach (var champion in _allChampions)
        {
            _allSkins.Add(
                champion, 
                new ObservableCollection<SkinViewModel>(await _dataInitService.LoadChampionSkinsAsync(champion, _selectedSkins)));
        }
    }

    private async Task OnChampionSelectedAsync()
    {
        if (SelectedChampionItem == null) return;

        var selectedChamp = _allChampions.FirstOrDefault(c => c.Name == SelectedChampionItem.Name);
        if (selectedChamp == null) return;
        
        DisplayedSkins = _allSkins.FirstOrDefault(x => x.Key.Id == selectedChamp.Id).Value;
    }

    private void SelectSkin(SkinViewModel? vm)
    {
        if (vm == null) return;

        if (!_filesystemService.IsSkinDownloaded(vm.Champion, vm.Model))
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
        _configService.SaveSelectedSkins(Config, _selectedSkins);

        if (_partyService != null && IsPartyModeEnabled)
        {
            _partyService.SelectedSkins[vm.Champion] = vm.Model;
            _ = _partyService.SendSkinDataToPartyAsync(vm.Model);
        }

        RunTool();
    }

    private void RunTool()
{
    if (_toolService == null) return;

    Task.Run(async () =>
    {
        try
        {
            foreach (var (champion, skin) in _selectedSkins)
            {
                var champIdStr = champion.Id.ToString();
                var skinIdStr = skin.Id.ToString();

                if (!skinIdStr.StartsWith(champIdStr))
                    continue;

                var skinIdShort = Convert.ToInt32(skinIdStr.Substring(champIdStr.Length));

                var championFolder = Path.Combine("Skins", champion.Name);

                string zipPath = Path.Combine(championFolder, $"skin{skinIdShort}.zip");

                var installPath = Path.Combine("installed", skin.Id.ToString());

                if (!Directory.Exists(installPath))
                {
                    if (File.Exists(zipPath))
                    {
                        _toolService.Import(zipPath, skin.Id.ToString());
                    }
                    else
                    {
                        _loggingService.Log($"Zip not found: {zipPath}");
                    }
                }
            }

            var selected = _selectedSkins.Values
                .Select(x => x.Id.ToString())
                .ToList();

            if (_customSkinService != null)
            {
                selected.AddRange(_customSkinService
                    .ImportedSkins
                    .Where(x => x.Enabled)
                    .Select(x => x.Name));
            }

            foreach (var id in selected)
                _loggingService.Log($"running with {id}");

            await _toolService.Run(
                selected.Where(x => Directory.Exists(Path.Combine("installed", x)))
            );
        }
        catch (Exception ex)
        {
            _loggingService.Log("Run Error: " + ex.Message);
        }
    });
}

    // private void RunTool()
    // {
    //     if (_toolService == null) return;
    //
    //     Task.Run(async () =>
    //     {
    //         try
    //         {
    //             foreach (var (champion, skin) in _selectedSkins)
    //             {
    //                 // Логика вычисления путей и импорта (можно вынести в Helper, но оставим тут для целостности логики инструмента)
    //                 var skinIdStr = skin.Id.ToString();
    //                 var champIdStr = champion.Id.ToString();
    //                 
    //                 // Убеждаемся, что skinId вычисляется корректно, как в оригинале
    //                 var skinId = Convert.ToInt32(skinIdStr.Substring(champIdStr.Length, skinIdStr.Length - champIdStr.Length));
    //
    //                 if (skin is SkinForm skinForm)
    //                 {
    //                     var skinPath = Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinId}", $"{skinForm.Stage}.fantome");
    //                     var installPath = Path.Combine("installed", $"{skin.Id}-{skinForm.Stage}");
    //                     
    //                     if (!Directory.Exists(installPath))
    //                         _toolService.Import(skinPath, $"{skin.Id}");
    //                 }
    //                 else
    //                 {
    //                     var skinPath = Path.Combine("skins", $"{champion.Id}", $"{skinId}.fantome");
    //                     var installPath = Path.Combine("installed", $"{skin.Id}");
    //
    //                     if (!Directory.Exists(installPath))
    //                         _toolService.Import(skinPath, $"{skin.Id}");
    //                 }
    //             }
    //             // Собираем список для запуска
    //             var selected = _selectedSkins.Values.Select(x => x.Id.ToString()).ToList();
    //             
    //             if (_customSkinService != null)
    //             {
    //                 selected.AddRange(_customSkinService.ImportedSkins.Where(x => x.Enabled).Select(x => x.Name));
    //             }
    //             
    //             // Запускаем
    //             foreach (var a in selected)
    //             {
    //                 _loggingService.Log($"running with {a}");
    //             }
    //             
    //            
    //             await _toolService.Run(selected.Where(x => Directory.Exists(Path.Combine("installed", x))));
    //         }
    //         catch (Exception ex)
    //         {
    //             _loggingService.Log("Run Error: " + ex.Message);
    //         }
    //     });
    // }

    // --- Вспомогательные Методы ---
    private void LoadChampionListBoxItems()
    {
        var items = new ObservableCollection<ChampionListItem>();
        foreach (var champion in _allChampions)
        {
            var iconPath = Path.Combine(new FileInfo(Environment.ProcessPath).DirectoryName, "assets", "champions", $"{champion.Id}.png");
            items.Add(new ChampionListItem(iconPath, champion.Name));
        }
        ChampionListItems = items;
    }

    private void FilterChampions()
    {
        var query = SearchText.ToLower();
        var filtered = _allChampions.Where(c => c.Name.ToLower().Contains(query));
        
        var list = new ObservableCollection<ChampionListItem>();
        foreach (var c in filtered)
        {
            // ИСПРАВЛЕНИЕ: Замена c.IconUrl на конструирование пути
            
            var iconPath = Path.Combine(new FileInfo(Environment.ProcessPath).DirectoryName, "assets", "champions", $"{c.Id}.png");
            list.Add(new ChampionListItem(iconPath, c.Name));
        }
        ChampionListItems = list;
    }

    private void LogToDebugText(string msg)
    {
        // TODO: remove this shit later, too much in dubug logs
        if(!msg.Contains("Modifying skin"))
            DebugText = DateTime.Now + " " + msg + "\n" + DebugText;
    }

    private void OpenSettings()
    {
        _navigationService.ShowDialog<SettingsViewModel>();
    }

    private void OpenCustomSkins()
    {
        _navigationService.ShowDialog<CustomSkinsViewModel>();
    }

    private void TogglePreloader(bool show)
    {
    }

    // --- Party Mode ---
    private async Task TogglePartyModeAsync()
    {
        if (IsPartyModeEnabled)
        {
            _selectedSkins = _savedSelectedSkins;
            _savedSelectedSkins = new Dictionary<Champion, Skin>();
            
            if (_partyService != null) 
                await _partyService.DisableAsync();
            
            LobbyId = "";
            LobbyStatus = "";
            Members = "";
        }
        else
        {
            IsBusy = true;
            if (_partyService != null) 
                await _partyService.EnableAsync(_selectedSkins);
            
            _savedSelectedSkins = _selectedSkins;
            _selectedSkins = new Dictionary<Champion, Skin>();
            _configService.SaveSelectedSkins(Config, _selectedSkins);

            IsBusy = false;
        }
        // Переключаем флаг
        IsPartyModeEnabled = !IsPartyModeEnabled;
    }

    private void RegisterPartyService()
    {
        if (_partyService != null) return;

        _partyService = new PartyService(_allChampions, _selectedSkins, Config.PartyModeUrl);
        _partyService.OnLog += _loggingService.Log;
        _partyService.OnError += msg => _navigationService.ShowCustomMessageBox("Error!", msg);
        
        _partyService.SkinRecieved += skin =>
        {
            try
            {
                _loggingService.Log("Recieved skin: " + skin.Name);
                // Используем .Any() для поиска чемпиона по скину
                var recievedChampion = _allChampions.FirstOrDefault(c => c.Skins.Any(x => x.Id == skin.Id));
                
                if (recievedChampion != null)
                {
                    _selectedSkins[recievedChampion] = skin;
                    
                    // Обновляем UI в главном потоке
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                         if (SelectedChampionItem != null && SelectedChampionItem.Name == recievedChampion.Name)
                            _ = OnChampionSelectedAsync();
                    });

                    RunTool();
                    _loggingService.Log("Successfully applied skin: " + skin.Name);
                }
            }
            catch (Exception ex)
            {
                _navigationService.ShowCustomMessageBox("Error!", ex.Message);
            }
        };

        _partyService.Enabled += () => { _loggingService.Log("Party mode enabled"); };
        
        _partyService.Disabled += () =>
        {
            _loggingService.Log("Party mode disabled");
            LobbyId = "";
            LobbyStatus = "";
            if (_partyService.BackupedSkins != null)
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
}