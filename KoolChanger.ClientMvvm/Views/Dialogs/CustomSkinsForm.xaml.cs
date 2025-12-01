#region

using System.Windows.Input;
using KoolChanger.ClientMvvm.Services;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;
using KoolChanger.Helpers;
using KoolChanger.Services;
// WindowBlurEffect, AccentState
// INavigationService (оставлен для конструктора)

#endregion

// ToolService (оставлен для конструктора)

namespace KoolChanger.ClientMvvm.Views.Dialogs;

public partial class CustomSkinsForm
{
    // --- Конструктор (значительно упрощен)

    // Примечание: В чистом MVVM View не должен знать о ToolService и NavigationService.
    // Они используются только для создания ViewModel (ViewModel First).
    // Если используется IoC-контейнер, эти параметры не нужны.
    public CustomSkinsForm(ToolService toolService, INavigationService navigationService)
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            // Применяем эффект размытия. 
            // DataContext НЕ перезаписывается и остается CustomSkinsViewModel.
            new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
            {
                BlurOpacity = 100
            };

            // Заметка: DataContext должен быть установлен вызывающим кодом
            // (например, new CustomSkinsForm().DataContext = new CustomSkinsViewModel(...)).
        };
    }

    // --- Методы View (только для работы окна и UI)

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            try
            {
                DragMove();
            }
            catch
            {
                // ignored
            }
    }

    // Эти методы больше не нужны, так как логика перенесена в команды в ViewModel:
    // private void DeleteSkin(object sender, MouseButtonEventArgs e) { ... }
    // private async void Border_Drop(object sender, DragEventArgs e) { ... }
    // private void Search(object sender, TextChangedEventArgs e) { ... }
    // private void Border_MouseDown(object sender, MouseButtonEventArgs e) { ... }

    // Используем CloseCommand из ViewModel
    private void Close(object sender, MouseButtonEventArgs e)
    {
        // Вместо прямого вызова Close(), вызываем команду, если она есть
        if (DataContext is CustomSkinsViewModel viewModel)
            if (viewModel.CloseCommand.CanExecute(null))
            {
                viewModel.CloseCommand.Execute(null);
                return;
            }

        // Резервный вариант, если команда не сработала или DataContext не установлен
        Close();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        // Оставить, если здесь есть логика, связанная только с WPF (например, очистка ресурсов UI)
    }
}