using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq; // Добавлен, чтобы убедиться, что LINQ-методы доступны
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Helpers;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Models;
using KoolChanger.Services;
// Обратите внимание: using KoolChanger.ClientMvvm.Helpers; удален, так как он,
// вероятно, больше не нужен после рефакторинга.
// using KoolChanger.ClientMvvm.Services; удален, так как он не требуется в ViewModel.


namespace KoolChanger.ClientMvvm.ViewModels;

public class MainViewModel : ObservableObject
{
    // --- Внедренные сервисы ---
    private readonly INavigationService _navigationService;
    private readonly IConfigService _configService;
    private readonly IFilesystemService _filesystemService;
    private readonly ILoggingService _loggingService;
    private readonly IDataInitializationService _dataInitService;

    // --- Существующие сервисы ---
    private readonly ChampionService _championService;
    private readonly UpdateService _updateService;
    private readonly KoolService _koolService;

    // --- Локальные сервисы и инструменты ---
    private ToolService? _toolService;
    private PartyService? _partyService;
    private CustomSkinService? _customSkinService;
    private Process _toolProcess = new();

    // --- Состояние данных ---
    private List<Champion> _allChampions = new();
    private Dictionary<Champion, Skin> _selectedSkins = new();
    private Dictionary<Champion, Skin> _savedSelectedSkins = new(); // Для Party Mode
    private Config Config { get; set; } = new();

    // --- Коллекции для View ---
    private ObservableCollection<ChampionListItem> _championListItems = new();
    private ObservableCollection<SkinViewModel> _displayedSkins = new();

    // --- Поля свойств ---
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

    // --- Конструктор ---
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
        // Присваивание зависимостей
        _championService = championService;
        _updateService = updateService;
        _navigationService = navigationService;
        _koolService = koolService;
        _configService = configService;
        _filesystemService = filesystemService;
        _loggingService = loggingService;
        _dataInitService = dataInitService;

        // Первоначальная настройка
        _koolService.Super();
        PreloaderViewModel = new PreloaderViewModel();

        // Подписки на события
        _loggingService.OnLog += LogToDebugText;
        _dataInitService.OnUpdating += data => BusyText = data;

        // Инициализация команд
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenCustomSkinsCommand = new RelayCommand(OpenCustomSkins);
        TogglePartyModeCommand = new AsyncRelayCommand(TogglePartyModeAsync);
        SelectSkinCommand = new RelayCommand<SkinViewModel>(SelectSkin);
        LoadedCommand = new AsyncRelayCommand(InitializeAsync);
        TogglePreloaderCommand = new RelayCommand<bool>(TogglePreloader);
    }

    // --- Свойства ViewModels ---
    public PreloaderViewModel PreloaderViewModel { get; }

    // --- Публичные свойства ---
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
                // Запускаем асинхронную задачу выбора чемпиона без блокировки сеттера
                _ = OnChampionSelectedAsync();
            }
        }
    }

    public ObservableCollection<SkinViewModel> DisplayedSkins
    {
        get => _displayedSkins;
        set => SetProperty(ref _displayedSkins, value);
    }

    // --- Команды ---
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenCustomSkinsCommand { get; }
    public IAsyncRelayCommand TogglePartyModeCommand { get; }
    public ICommand SelectSkinCommand { get; }
    public IAsyncRelayCommand LoadedCommand { get; }
    public ICommand TogglePreloaderCommand { get; }

    // --- Логика Инициализации ---
    private async Task InitializeAsync()
    {
        // Ждем, пока UI поток освободится для корректной работы биндингов
        await Task.Yield();
        IsBusy = true;

        // 1. Инициализация файловой системы
        BusyText = "Initializing folders and files...";
        await _filesystemService.InitializeFoldersAndFilesAsync();

        // 2. Загрузка конфигурации
        Config = _configService.LoadConfig();

        // 3. Загрузка данных чемпионов и ассетов
        BusyText = "Loading champion data and assets...";
        await _dataInitService.InitializeDataAsync(Config);
        _allChampions = _dataInitService.AllChampions;

        // 4. Восстановление выбранных скинов из конфига
        _selectedSkins = _configService.LoadSelectedSkins(_allChampions);

        // 5. Проверка пути к игре и сохранение конфига
        _configService.InitializeGamePath(Config);
        _configService.SaveConfig(Config);

        // 6. Загрузка списка чемпионов в UI
        LoadChampionListBoxItems();

        // 7. Инициализация ToolService
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

        // Если путь к игре корректен и есть выбранные скины, запускаем инструмент
        if (!string.IsNullOrEmpty(Config.GamePath) && _selectedSkins.Count > 0)
            RunTool();

        PreloaderViewModel.Status = string.Empty;
        IsBusy = false;

        if (_filesystemService.IsFirstRun())
        {
            // Логика первого запуска (если есть)
        }

        RegisterPartyService();
        LoadChampionListBoxItems();
    }

    // --- Логика Выбора ---
    private async Task OnChampionSelectedAsync()
    {
        if (SelectedChampionItem == null) return;

        var selectedChamp = _allChampions.FirstOrDefault(c => c.Name == SelectedChampionItem.Name);
        if (selectedChamp == null) return;

        DisplayedSkins.Clear();

        // Загружаем скины через сервис данных
        var skins = await _dataInitService.LoadChampionSkinsAsync(selectedChamp, _selectedSkins);
        
        // Обновляем коллекцию UI
        DisplayedSkins = new ObservableCollection<SkinViewModel>(skins);
    }

    private void SelectSkin(SkinViewModel? vm)
    {
        if (vm == null) return;

        // Проверяем наличие файлов скина
        if (!_filesystemService.IsSkinDownloaded(vm.Champion, vm.Model))
        {
            _navigationService.ShowCustomMessageBox("Error!",
                $"This skin does not exists.\nTry to re-download skins.\nCurrent skin id - {vm.Model.Id}");
            return;
        }

        // Обновляем визуальное выделение
        foreach (var s in DisplayedSkins)
        {
            s.IsSelected = false;
            foreach (var child in s.Children) child.IsSelected = false;
        }

        vm.IsSelected = true;

        // Сохраняем выбор
        _selectedSkins[vm.Champion] = vm.Model;
        _configService.SaveSelectedSkins(Config, _selectedSkins);

        // Отправляем данные в Party Mode
        if (_partyService != null && IsPartyModeEnabled)
        {
            _partyService.SelectedSkins[vm.Champion] = vm.Model;
            _ = _partyService.SendSkinDataToPartyAsync(vm.Model);
        }

        RunTool();
    }

    // --- Логика Запуска Инструмента ---
    private void RunTool()
    {
        if (_toolService == null) return;

        Task.Run(async () =>
        {
            try
            {
                foreach (var (champion, skin) in _selectedSkins)
                {
                    // Логика вычисления путей и импорта (можно вынести в Helper, но оставим тут для целостности логики инструмента)
                    var skinIdStr = skin.Id.ToString();
                    var champIdStr = champion.Id.ToString();
                    
                    // Убеждаемся, что skinId вычисляется корректно, как в оригинале
                    var skinId = Convert.ToInt32(skinIdStr.Substring(champIdStr.Length, skinIdStr.Length - champIdStr.Length));

                    if (skin is SkinForm skinForm)
                    {
                        var skinPath = Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinId}", $"{skinForm.Stage}.fantome");
                        var installPath = Path.Combine("installed", $"{skin.Id}-{skinForm.Stage}");
                        
                        if (!Directory.Exists(installPath))
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }
                    else
                    {
                        var skinPath = Path.Combine("skins", $"{champion.Id}", $"{skinId}.fantome");
                        var installPath = Path.Combine("installed", $"{skin.Id}");

                        if (!Directory.Exists(installPath))
                            _toolService.Import(skinPath, $"{skin.Id}");
                    }
                }
                // Собираем список для запуска
                var selected = _selectedSkins.Values.Select(x => x.Id.ToString()).ToList();
                
                if (_customSkinService != null)
                {
                    selected.AddRange(_customSkinService.ImportedSkins.Where(x => x.Enabled).Select(x => x.Name));
                }
                
                // Запускаем
                foreach (var a in selected)
                {
                    _loggingService.Log($"running with {a}");
                }
                
               
                await _toolService.Run(selected.Where(x => Directory.Exists(Path.Combine("installed", x))));
            }
            catch (Exception ex)
            {
                _loggingService.Log("Run Error: " + ex.Message);
            }
        });
    }

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
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

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
        DebugText = msg + "\n" + DebugText;
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