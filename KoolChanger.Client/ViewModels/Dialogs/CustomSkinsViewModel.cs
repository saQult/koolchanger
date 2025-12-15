#region

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.Client.Interfaces;
using KoolChanger.Core.Models;
using KoolChanger.Core.Services;

#endregion

namespace KoolChanger.Client.ViewModels.Dialogs;

public class CustomSkinsViewModel : ObservableObject
{
    private readonly CustomSkinService _customSkinService;
    private readonly INavigationService _navigationService;
    private readonly ICollectionView _view;

    private string _busyText = string.Empty;

    private bool _isBusy;

    private string _searchText = string.Empty;
    public CustomSkinsViewModel(
        IToolServiceFactory toolServiceFactory,
        ICustomSkinServiceFactory customSkinServiceFactory,
        IConfigService configService,
        INavigationService navigationService)
    {
        _navigationService = navigationService;
        var config = configService.LoadConfig();
        var toolService = toolServiceFactory.Create(config.GamePath);
        _customSkinService = customSkinServiceFactory.Create(toolService);

        foreach (var s in _customSkinService.ImportedSkins)
            SkinListboxItems.Add(new CustomSkinListBoxItem(s));

        _view = CollectionViewSource.GetDefaultView(SkinListboxItems);
        _view.Filter = FilterSkins;

        CloseCommand = new RelayCommand(() => _navigationService.CloseWindow(this));
        DropCommand = new AsyncRelayCommand<string[]>(ExecuteDropAsync!);
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

    public ObservableCollection<CustomSkinListBoxItem> SkinListboxItems { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _view.Refresh();
        }
    }

    public ICommand CloseCommand { get; }
    public ICommand DropCommand { get; }
    public ICommand DeleteSkinCommand { get; }
    public ICommand ToggleSkinEnabledCommand { get; }

    
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

    private bool FilterSkins(object obj)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        if (obj is not CustomSkinListBoxItem item) return false;

        var q = SearchText.ToLowerInvariant();
        return item.RealName.ToLowerInvariant().Contains(q) || item.Text.ToLowerInvariant().Contains(q);
    }
}

public class CustomSkinListBoxItem : INotifyPropertyChanged
{
    private bool _enabled;

    public CustomSkinListBoxItem(CustomSkin skin)
    {
        RealName = skin.Name;
        Name = skin.Name.Length > 15 ? skin.Name[..15] + "..." : skin.Name;
        skin.Version = string.IsNullOrEmpty(skin.Version) ? "1.0.0" : skin.Version;
        skin.Author = string.IsNullOrEmpty(skin.Author) ? "unknown" : skin.Author;

        var headerText = $"v{skin.Version} by {skin.Author}";
        headerText = headerText.Length > 30 ? headerText[..30] + "..." : headerText;

        var bottomText = skin.Description.Length > 30 ? skin.Description[..30] + "..." : skin.Description;

        Text = string.IsNullOrEmpty(bottomText) ? headerText : headerText + Environment.NewLine + bottomText;

        Enabled = skin.Enabled;
    }

    public string RealName { get; }
    public string Name { get; }
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
