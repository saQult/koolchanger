#region

using System.Windows;
using System.Windows.Input;

#endregion

namespace KoolChanger.Client.Behaviors;

public static class DropFileBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(DropFileBehavior),
            new PropertyMetadata(null, OnCommandChanged));

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

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement uiElement)
        {
            if (e.OldValue is ICommand) uiElement.Drop -= OnDrop;

            if (e.NewValue is ICommand)
            {
                uiElement.Drop += OnDrop;
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

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];

        if (command != null && command.CanExecute(files))
        {
            command.Execute(files);
            e.Handled = true;
        }
    }

    private static void OnPreviewDragOver(object sender, DragEventArgs e)
    {
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