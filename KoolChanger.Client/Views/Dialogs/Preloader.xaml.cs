#region

using System;
using System.Windows;
using KoolChanger.Client.ViewModels.Dialogs;

#endregion

namespace KoolChanger.Client.Views.Dialogs;

public partial class Preloader : Window
{
    public Preloader(PreloaderViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;
    }

    public PreloaderViewModel ViewModel { get; }
}
