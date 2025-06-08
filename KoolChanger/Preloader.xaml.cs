using KoolChanger.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KoolChanger
{
    /// <summary>
    /// Логика взаимодействия для Preloader.xaml
    /// </summary>
    public partial class Preloader : Window
    {
        public Preloader()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
                {
                    BlurOpacity = 100
                };
            };
        }

        public void SetStatus(string text) => Dispatcher.Invoke(() => StatusLabel.Content = text);
    }
}
