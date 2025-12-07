using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Services;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface INavigationService
{
    // Регистрация связки ViewModel -> Window
    void Register<TViewModel, TView>() 
        where TViewModel : class 
        where TView : System.Windows.Window;

    // Навигация (для обычных окон)
    void NavigateTo<TViewModel>() where TViewModel : class;

    // Показ диалога (универсальный)
    void ShowDialog<TViewModel>() where TViewModel : class;

    // Специфические методы (для удобства вызова из MainViewModel)
    void ShowSettings();
    void ShowCustomSkins(CustomSkinService? customSkinService = null); // Параметр опционален, если сервис есть в DI

    // Сообщение
    CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message);
    
    // Закрытие
    void CloseWindow(object viewModel);
}