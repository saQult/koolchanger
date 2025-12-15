#region

using KoolChanger.Client.Interfaces;
using KoolChanger.Client.ViewModels;
using KoolChanger.Client.ViewModels.Dialogs;
using KoolChanger.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Effects;

#endregion

namespace KoolChanger.Client.Services;

public class NavigationService : INavigationService, INotifyPropertyChanged
{
    private readonly Dictionary<Type, Type> _mappings = new();
    private readonly Dictionary<Type, Type> _pageMappings = new();
    private readonly Dictionary<Type, Page> _pageCache = new();
    private readonly IServiceProvider _serviceProvider;
    private Page? _currentPage;

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
        InternalShow<TViewModel>(isDialog: false);
    }

    public void ShowDialog<TViewModel>() where TViewModel : class
    {
        InternalShow<TViewModel>(isDialog: true);
    }

    public void ShowSettings()
    {
        ShowDialog<SettingsViewModel>();
    }

    public void ShowCustomSkins(CustomSkinService? customSkinService = null)
    {
        ShowDialog<CustomSkinsViewModel>();
    }

    public CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message)
    {
        var viewModel = new CustomMessageBoxViewModel(header, message, this);

        if (!_mappings.TryGetValue(typeof(CustomMessageBoxViewModel), out var viewType))
            throw new InvalidOperationException($"No registered View found for ViewModel: {typeof(CustomMessageBoxViewModel).Name}");

        var window = _serviceProvider.GetService(viewType) as Window;

        if (window == null)
            window = (Window)Activator.CreateInstance(viewType)!;

        ConfigureWindow(window, viewModel);
        window.ShowDialog();

        return viewModel;
    }

    public void CloseWindow(object viewModel)
    {
        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            if (window.DataContext == viewModel)
            {
                window.Close();
                break;
            }
        }
    }

    private void InternalShow<TViewModel>(bool isDialog) where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);

        if (!_mappings.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No registered View for ViewModel: {viewModelType.Name}.");

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        var window = _serviceProvider.GetRequiredService(viewType) as Window;
        if (window == null)
            window = (Window)Activator.CreateInstance(viewType)!;

        ConfigureWindow(window, viewModel, applyBlurToOwner: isDialog);

        if (isDialog)
            window.ShowDialog();
        else
            window.Show();
    }

    private void ConfigureWindow(Window window, object viewModel, bool applyBlurToOwner = false)
    {
        var activeWindow = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (activeWindow != null && activeWindow != window)
        {
            window.Owner = activeWindow;

            if (applyBlurToOwner)
            {
                var owner = activeWindow;
                var previousEffect = owner.Effect;

                owner.Effect = new BlurEffect
                {
                    Radius = 10
                };

                window.Closed += (_, __) =>
                {
                    owner.Effect = previousEffect;
                };
            }
        }

        window.DataContext = viewModel;
    }

    public void RegisterPage<TViewModel, TPage>()
        where TViewModel : class
        where TPage : Page
    {
        _pageMappings[typeof(TViewModel)] = typeof(TPage);
    }

    public void NavigateToPage<TViewModel>() where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);

        if (!_pageMappings.TryGetValue(viewModelType, out var pageType))
            throw new InvalidOperationException($"No registered Page for key: {viewModelType.Name}");

        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        if (!_pageCache.TryGetValue(viewModelType, out var page))
        {
            page = _serviceProvider.GetService(pageType) as Page;
            if (page == null)
                page = (Page?)Activator.CreateInstance(pageType);

            if (page == null)
                throw new InvalidOperationException($"Unable to create Page instance for key: {viewModelType.Name}");

            page.DataContext = viewModel;
            _pageCache[viewModelType] = page;
        }

        CurrentPage = page;
    }

    public Page? CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (!ReferenceEquals(_currentPage, value))
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}