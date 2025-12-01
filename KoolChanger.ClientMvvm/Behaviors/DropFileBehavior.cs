#region

using System.Windows;
using System.Windows.Input;

#endregion

namespace KoolChanger.ClientMvvm.Behaviors;

// Этот класс позволяет привязать команду из ViewModel к событию Drop элемента View
public static class DropFileBehavior
{
    // 1. Регистрируем Attached Property для команды (ICommand)
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(DropFileBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    // 2. Регистрируем Attached Property для параметра команды (CommandParameter)
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(DropFileBehavior),
            new PropertyMetadata(null));

    public static ICommand GetCommand(DependencyObject obj)
    {
        return (ICommand)obj.GetValue(CommandProperty);
    }

    public static void SetCommand(DependencyObject obj, ICommand value)
    {
        obj.SetValue(CommandProperty, value);
    }

    public static object GetCommandParameter(DependencyObject obj)
    {
        return (object)obj.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(DependencyObject obj, object value)
    {
        obj.SetValue(CommandParameterProperty, value);
    }

    // 3. Метод, который вызывается при установке Attached Property (Command)
    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement uiElement)
        {
            // Отписываемся от старого обработчика и подписываемся на новый
            if (e.OldValue is ICommand) uiElement.Drop -= OnDrop;

            if (e.NewValue is ICommand)
            {
                uiElement.Drop += OnDrop;
                // Обязательно подписываемся на PreviewDragOver, чтобы разрешить Drop
                uiElement.PreviewDragOver += OnPreviewDragOver;
            }
        }
    }

    // 4. Обработчик события Drop
    private static void OnDrop(object sender, DragEventArgs e)
    {
        if (sender is not UIElement uiElement) return;

        var command = GetCommand(uiElement);
        var parameter = GetCommandParameter(uiElement);

        // Получаем список путей к файлам
        var files = e.Data.GetData(DataFormats.FileDrop) as string[];

        if (command != null && command.CanExecute(files))
        {
            // Передаем массив путей к файлам в команду ViewModel
            command.Execute(files);
            e.Handled = true;
        }
    }

    // 5. Обработчик для разрешения DragOver
    private static void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        // Обязательно разрешаем Drop, иначе событие Drop не сработает
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }
}