#region

using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Services; // Для CustomSkinService, если он нужен
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Effects;

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
        foreach (Window window in Application.Current.Windows)
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
        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
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
}