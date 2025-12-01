#region

using System.Windows;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;

#endregion

namespace KoolChanger.ClientMvvm.Views.Dialogs;

public partial class Preloader : Window
{
    public Preloader()
    {
        InitializeComponent();
        ViewModel = (PreloaderViewModel)DataContext;
    }

    public PreloaderViewModel ViewModel { get; }
}