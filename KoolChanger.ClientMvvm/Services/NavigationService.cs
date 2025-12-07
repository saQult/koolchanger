#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Services; // Для CustomSkinService, если он нужен
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
        InternalShow<TViewModel>(isDialog: false);
    }

    public void ShowDialog<TViewModel>() where TViewModel : class
    {
        InternalShow<TViewModel>(isDialog: true);
    }

    // Реализация конкретного метода для настроек
    public void ShowSettings()
    {
        // Мы просто вызываем универсальный метод. 
        // SettingsViewModel должна получать IConfigService через конструктор автоматически.
        ShowDialog<SettingsViewModel>();
    }

    // Реализация конкретного метода для кастомных скинов
    public void ShowCustomSkins(CustomSkinService? customSkinService = null)
    {
        // Если CustomSkinsViewModel требует передачи параметра, которого нет в DI,
        // нам пришлось бы использовать фабрику. 
        // Но лучше зарегистрировать CustomSkinService как Singleton/Scoped в DI.
        // Тогда этот метод тоже превращается просто в:
        ShowDialog<CustomSkinsViewModel>();
    }

    public CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message)
    {
        // MessageBox - исключение, так как он принимает динамические строки (header, message)
        var viewModel = new CustomMessageBoxViewModel(header, message, this);

        if (!_mappings.TryGetValue(typeof(CustomMessageBoxViewModel), out var viewType))
            throw new InvalidOperationException($"No registered View found for ViewModel: {typeof(CustomMessageBoxViewModel).Name}");

        // Получаем Window из DI (чтобы сработали стили и т.д., если оно там есть)
        // Если Window не зарегистрировано в DI как Transient, можно использовать Activator.CreateInstance
        var window = _serviceProvider.GetService(viewType) as Window;
        
        // Fallback если окно не зарегистрировано в DI, но есть в маппинге
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

    // --- Private Helpers ---

    private void InternalShow<TViewModel>(bool isDialog) where TViewModel : class
    {
        var viewModelType = typeof(TViewModel);

        if (!_mappings.TryGetValue(viewModelType, out var viewType))
            throw new InvalidOperationException($"No registered View for ViewModel: {viewModelType.Name}.");

        // 1. Получаем ViewModel из DI. Все зависимости (IConfigService, UpdateService и т.д.) внедрятся сами.
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        // 2. Получаем View из DI.
        var window = _serviceProvider.GetRequiredService(viewType) as Window;

        if (window != null)
        {
            ConfigureWindow(window, viewModel);

            if (isDialog)
                window.ShowDialog();
            else
                window.Show();
        }
    }

    private void ConfigureWindow(Window window, object viewModel)
    {
        // Установка владельца
        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        if (activeWindow != null && activeWindow != window) 
        {
            window.Owner = activeWindow;
        }

        window.DataContext = viewModel;
    }
}