#region

using System.Windows;
using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.ClientMvvm.Views.Dialogs;
using KoolChanger.Services;
using Microsoft.Extensions.DependencyInjection;

#endregion

namespace KoolChanger.ClientMvvm.Services;

public class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Type> _mappings = new();
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Register<TViewModel, TView>()
        where TViewModel : class
        where TView : Window
    {
        _mappings[typeof(TViewModel)] = typeof(TView);
    }

    public void NavigateTo<TViewModel>() where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);

        if (!_mappings.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No registered View for ViewModel: {viewModelType.Name}.");

        var viewModel = _serviceProvider.GetRequiredService(viewModelType);
        var window = _serviceProvider.GetRequiredService(viewType) as Window;

        if (window != null)
        {
            window.DataContext = viewModel;
            window.Show();
        }
    }

    public void ShowDialog<TViewModel>() where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);

        if (viewModelType == typeof(CustomSkinsViewModel))
        {
            ShowCustomSkinsDialog();
            return;
        }

        if (viewModelType == typeof(SettingsViewModel))
        {
            ShowSettingsDialog();
            return;
        }

        if (!_mappings.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No registered View for ViewModel: {viewModelType.Name}.");

        var viewModel = _serviceProvider.GetRequiredService(viewModelType);
        var window = _serviceProvider.GetRequiredService(viewType) as Window;

        if (window != null)
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != null && activeWindow != window) window.Owner = activeWindow;

            window.DataContext = viewModel;
            window.ShowDialog();
        }
    }

    public void CloseWindow(object viewModel)
    {
        foreach (Window window in Application.Current.Windows)
            if (window.DataContext == viewModel)
            {
                window.Close();
                break;
            }
    }

    public CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message)
    {
        var navService = _serviceProvider.GetRequiredService<INavigationService>();
        var viewModel = new CustomMessageBoxViewModel(header, message, navService);

        if (!_mappings.TryGetValue(typeof(CustomMessageBoxViewModel), out var viewType))
            throw new InvalidOperationException(
                $"No registered View found for ViewModel: {typeof(CustomMessageBoxViewModel).Name}");

        var window = _serviceProvider.GetRequiredService(viewType) as Window;

        if (window != null)
        {
            var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            if (activeWindow != null && activeWindow != window) window.Owner = activeWindow;

            window.DataContext = viewModel;
            window.ShowDialog();

            return viewModel;
        }

        throw new InvalidOperationException("Failed to open CustomMessageBox.");
    }

    private void ShowCustomSkinsDialog()
    {
        var toolService = _serviceProvider.GetRequiredService<ToolService>();
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        var viewModel = new CustomSkinsViewModel(toolService, navigationService);
        var window = new CustomSkinsForm(toolService, navigationService);

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (activeWindow != null && activeWindow != window) window.Owner = activeWindow;

        window.DataContext = viewModel;
        window.ShowDialog();
    }

    private void ShowSettingsDialog()
    {
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var updateService = _serviceProvider.GetRequiredService<UpdateService>();
        var skinService = _serviceProvider.GetRequiredService<SkinService>();
        var championService = _serviceProvider.GetRequiredService<ChampionService>();
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        var viewModel = new SettingsViewModel(mainViewModel.Config.GamePath, updateService, skinService,
            championService, navigationService);
        viewModel.GamePathChanged += newPath =>
        {
            mainViewModel.Config.GamePath = newPath;
            mainViewModel.SaveConfig();
        };

        var window = new SettingsForm();

        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (activeWindow != null && activeWindow != window) window.Owner = activeWindow;

        window.DataContext = viewModel;
        window.ShowDialog();
    }
}