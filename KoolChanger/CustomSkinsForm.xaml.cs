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
    /// Логика взаимодействия для CustomSkinsForm.xaml
    /// </summary>
    public partial class CustomSkinsForm : Window
    {
        public CustomSkinsForm()
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

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Assuming you have one file that you care about, pass it off to whatever
                // handling code you have defined.
                new CustomMessageBox("asd", files[0], this).ShowDialog();
            }
        }
    }
}
