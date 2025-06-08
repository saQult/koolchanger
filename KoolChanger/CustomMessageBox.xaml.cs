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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KoolChanger
{
    /// <summary>
    /// Логика взаимодействия для CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {

        public CustomMessageBox(string header, string message, Window owner)
        {
            InitializeComponent();
            Header.Text = header;
            Message.Text = message;

            Loaded += (_, _) =>
            {
                DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
                {
                    BlurOpacity = 100
                };
            };
            if(owner != null)
            {
                Owner = owner;
                Owner.Effect = new BlurEffect { Radius = 10 };
            }
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            if(Owner != null)
                Owner.Effect = null;
            Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            if (Owner != null)
                Owner.Effect = null;
            Close();
        }
    }
}
