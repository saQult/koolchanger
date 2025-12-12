#region

using System;
using System.Windows;
using KoolChanger.ClientMvvm.ViewModels.Dialogs;

#endregion

namespace KoolChanger.ClientMvvm.Views.Dialogs;

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
