using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.Client.Models;
using System.Collections.ObjectModel;
using KoolChanger.Client.Views.Pages;
using KoolChanger.Client.Interfaces;
using KoolChanger.Client.ViewModels.Dialogs;

namespace KoolChanger.Client.ViewModels;

public partial class TestViewModel : ObservableObject
{
    public ObservableCollection<NavItem> SidebarItems { get; } = new();

    public object? CurrentPage => _navigationService.CurrentPage;

    private readonly INavigationService _navigationService;

    public TestViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.PropertyChanged += (_, __) => OnPropertyChanged(nameof(CurrentPage));

        BuildSidebar();
        SetupNavigation();
    }

    private void BuildSidebar()
    {
        SidebarItems.Add(new NavItem("Skins", null,
            new NavItem("Skins by champions", "skins-by-champion"),
            new NavItem("Favourites", "skins-favourite"),
            new NavItem("All skins", "skins-all"),
            new NavItem("Custom skins", "skins-custom")
        ));

        SidebarItems.Add(new NavItem("Settings", null,
            new NavItem("Settings", "settings")
        ));
    }

    private void SetupNavigation()
    {
        var navigateCommand = new RelayCommand<NavItem>(NavigateToItem);

        foreach (var item in SidebarItems)
        {
            SetupNavigationRecursive(item, navigateCommand);
        }
    }

    private void SetupNavigationRecursive(NavItem item, RelayCommand<NavItem> navigateCommand)
    {
        item.NavigateCommand = navigateCommand;

        foreach (var child in item.Children)
        {
            SetupNavigationRecursive(child, navigateCommand);
        }
    }

    private void NavigateToItem(NavItem? item)
    {
        if (item == null || !item.IsLeaf)
            return;
        switch (item.Key)
        {
            case "skins-by-champion":
                _navigationService.NavigateToPage<SkinsPageViewModel>(); break;
            case "skins-custom":
                _navigationService.NavigateToPage<CustomSkinsViewModel>(); break;
            case "settings":
                _navigationService.NavigateToPage<SettingsViewModel>(); break;
        }

    }
}
