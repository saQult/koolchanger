using KoolChanger.Client.ViewModels;
using KoolChanger.Client.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace KoolChanger.Client.Views.Pages
{
    /// <summary>
    /// Логика взаимодействия для SkinsPage.xaml
    /// </summary>
    public partial class SkinsPage : Page
    {
        private Preloader? _preloader;
        public SkinsPage()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {

                if (DataContext is SkinsPageViewModel vm)
                {
                    _preloader = new Preloader(vm.PreloaderViewModel)
                    {
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };

                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(SkinsPageViewModel.IsBusy))
                            Dispatcher.Invoke(() =>
                            {
                                if (vm.IsBusy)
                                {
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
        }
    }
}
