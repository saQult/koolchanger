#region

using System.Windows;
using KoolChanger.ClientMvvm.ViewModels;

#endregion

namespace KoolChanger.ClientMvvm.Services;

// Интерфейс для навигации
public interface INavigationService
{
    // Методы для навигации между окнами или для показа диалоговых окон
    void NavigateTo<TViewModel>() where TViewModel : class;
    void ShowDialog<TViewModel>() where TViewModel : class;
    void CloseWindow(object viewModel); // Закрыть окно, связанное с данным ViewModel

    CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message);

    void Register<TViewModel, TView>()
        where TViewModel : class
        where TView : Window;
}