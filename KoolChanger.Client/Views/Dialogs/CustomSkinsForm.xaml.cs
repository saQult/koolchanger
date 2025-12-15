#region

using System;
using System.Windows;
using System.Windows.Input;
using KoolChanger.Client.ViewModels.Dialogs;
using KoolChanger.Core.Helpers;

#endregion

namespace KoolChanger.Client.Views.Dialogs;

public partial class CustomSkinsForm : Window
{
    public CustomSkinsForm()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
            {
                BlurOpacity = 100
            };
        };
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            try
            {
                DragMove();
            }
            catch (Exception)
            {
            }
        }
    }

    private void Close(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is CustomSkinsViewModel viewModel &&
            viewModel.CloseCommand.CanExecute(null))
        {
            viewModel.CloseCommand.Execute(null);
            return;
        }

        Close();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
    }
}
