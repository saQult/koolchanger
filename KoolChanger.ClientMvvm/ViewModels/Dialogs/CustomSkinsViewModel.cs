#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.Services;
using KoolChanger.Models;
using KoolChanger.Services;
// INavigationService
// CustomSkin

#endregion

namespace KoolChanger.ClientMvvm.ViewModels.Dialogs;

public class CustomSkinsViewModel : ObservableObject
{
    // --- Поля и Сервисы
    private readonly CustomSkinService _customSkinService;
    private readonly INavigationService _navigationService;
    private readonly ICollectionView _view;

    private string _busyText = string.Empty;

    // --- Свойства для привязки (Busy Indicator)
    private bool _isBusy;

    private string _searchText = string.Empty;

    // --- Конструктор
    public CustomSkinsViewModel(ToolService toolService, INavigationService navigationService)
    {
        _navigationService = navigationService;
        _customSkinService = new CustomSkinService(toolService);

        // Инициализация коллекций и фильтрации
        foreach (var s in _customSkinService.ImportedSkins)
            SkinListboxItems.Add(new CustomSkinListBoxItem(s));

        // View для фильтрации данных (остается в VM)
        _view = CollectionViewSource.GetDefaultView(SkinListboxItems);
        _view.Filter = FilterSkins;

        // Инициализация команд
        CloseCommand = new RelayCommand(() => _navigationService.CloseWindow(this));
        DropCommand = new AsyncRelayCommand<string[]>(ExecuteDropAsync);
        DeleteSkinCommand = new RelayCommand<CustomSkinListBoxItem>(ExecuteDeleteSkin);
        ToggleSkinEnabledCommand = new RelayCommand<CustomSkinListBoxItem>(ExecuteToggleSkinEnabled);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string BusyText
    {
        get => _busyText;
        set => SetProperty(ref _busyText, value);
    }

    // --- Свойства для привязки (List & Search)
    public ObservableCollection<CustomSkinListBoxItem> SkinListboxItems { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                // Логика фильтрации теперь находится здесь, в ViewModel
                _view.Refresh();
        }
    }

    // --- Команды
    public ICommand CloseCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand DeleteSkinCommand { get; }
    public ICommand ToggleSkinEnabledCommand { get; }

    // --- Методы команд

    private async Task ExecuteDropAsync(string[] files)
    {
        if (files is null || files.Length == 0) return;

        IsBusy = true;

        try
        {
            foreach (var file in files)
            {
                BusyText = "Extracting: " + file;

                await Task.Run(() =>
                {
                    var skin = _customSkinService.FromFile(file);

                    if (_customSkinService.ImportedSkins.Any(s => s.Name == skin.Name))
                        return;

                    _customSkinService.AddSkin(skin, file);
                    Application.Current.Dispatcher.Invoke(() =>
                        SkinListboxItems.Add(new CustomSkinListBoxItem(skin)));
                });
            }
        }
        catch (Exception ex)
        {
            _navigationService.ShowCustomMessageBox("Error!", ex.Message);
        }
        finally
        {
            IsBusy = false;
            BusyText = string.Empty;
        }
    }

    private void ExecuteDeleteSkin(CustomSkinListBoxItem item)
    {
        if (item is null) return;

        var skin = _customSkinService.ImportedSkins.FirstOrDefault(x => x.Name == item.RealName);
        if (skin is null) return;

        _customSkinService.RemoveSkin(skin);
        SkinListboxItems.Remove(item);
    }

    private void ExecuteToggleSkinEnabled(CustomSkinListBoxItem item)
    {
        if (item is null) return;

        var skin = _customSkinService.ImportedSkins.FirstOrDefault(x => x.Name == item.RealName);
        if (skin is null) return;

        var newValue = !item.Enabled;
        item.Enabled = newValue;
        skin.Enabled = newValue;
        _customSkinService.SaveSkins();
    }

    // --- Метод фильтрации
    private bool FilterSkins(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        if (obj is not CustomSkinListBoxItem item) return false;

        var q = SearchText.ToLowerInvariant();
        return item.RealName.ToLowerInvariant().Contains(q) || item.Text.ToLowerInvariant().Contains(q);
    }
}

// CustomSkinListBoxItem остается здесь или в отдельном файле, 
// так как это модель элемента представления, связанная с INotifyPropertyChanged
public class CustomSkinListBoxItem : INotifyPropertyChanged
{
    private bool _enabled;

    public CustomSkinListBoxItem(CustomSkin skin)
    {
        RealName = skin.Name;
        skin.Version = string.IsNullOrEmpty(skin.Version) ? "1.0.0" : skin.Version;
        skin.Author = string.IsNullOrEmpty(skin.Author) ? "unknown" : skin.Author;

        var headerText = $"v{skin.Version} by {skin.Author}";
        headerText = headerText.Length > 30 ? headerText[..30] + "..." : headerText;

        var bottomText = skin.Description.Length > 30 ? skin.Description[..30] + "..." : skin.Description;

        Text = string.IsNullOrEmpty(bottomText) ? headerText : headerText + Environment.NewLine + bottomText;

        Enabled = skin.Enabled;
    }

    public string RealName { get; }
    public string Text { get; }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}