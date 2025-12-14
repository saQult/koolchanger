#region

using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;
using KoolChanger.ClientMvvm.ViewModels;
using KoolChanger.ClientMvvm.Views.Dialogs;
using KoolChanger.Helpers;

#endregion

namespace KoolChanger.ClientMvvm.Views.Windows;

public partial class MainWindow : Window
{
    private Preloader? _preloader;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND) { BlurOpacity = 100 };

            if (DataContext is MainViewModel vm)
            {
                _preloader = new Preloader(vm.PreloaderViewModel)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                vm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.IsBusy))
                        Dispatcher.Invoke(() =>
                        {
                            if (vm.IsBusy)
                            {
                                Effect = new BlurEffect { Radius = 10 };
                                _preloader?.Show();
                            }
                            else
                            {
                                Effect = null;
                                _preloader?.Hide();
                            }
                        });
                };
            }
        };

        Closed += (_, _) =>
        {
            _preloader?.Close();
            Application.Current.Shutdown();
        };
    }

    private void DragMove(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();

        }
        catch (Exception)
        {
        }
    }

    private void Minimize_Click(object sender, MouseButtonEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseApp_Click(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    private void OpenSettings_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.OpenSettingsCommand.Execute(null);
    }

    private void CheckForCombinations(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            debugColum.Width = debugColum.Width.IsAbsolute && debugColum.Width.Value == 0
                ? new GridLength(350)
                : new GridLength(0);
            Width = Width == 1160 ? 1510 : 1160;
        }
    }
}
