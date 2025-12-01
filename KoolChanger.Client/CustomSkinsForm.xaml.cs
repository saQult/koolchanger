using KoolChanger.Models;
using KoolChanger.Services;
using KoolChanger.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Threading;

namespace KoolChanger
{
    /// <summary>
    /// Логика взаимодействия для CustomSkinsForm.xaml
    /// </summary>
    public partial class CustomSkinsForm : Window
    {
        private readonly Preloader _preloader = new();
        private readonly CustomSkinService _customSkinService;
        private readonly ToolService _toolService;
        private readonly ObservableCollection<CustomSkinListBoxItem> _skinListboxItems = new();
        public CustomSkinsForm(ToolService toolService)
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                DataContext = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
                {
                    BlurOpacity = 100
                };
            };

            _toolService = toolService;
            _customSkinService = new(_toolService);

            foreach (var s in _customSkinService.ImportedSkins)
                _skinListboxItems.Add(new CustomSkinListBoxItem(s));

            customSkinsListBox.ItemsSource = _skinListboxItems;
            _view = CollectionViewSource.GetDefaultView(_skinListboxItems);
        }
        private ICollectionView _view;
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        private void DeleteSkin(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image img) return;
            if (img.DataContext is not CustomSkinListBoxItem item) return;

            var skin = _customSkinService.ImportedSkins.FirstOrDefault(x => x.Name == item.RealName);
            if (skin is null) return;

            _customSkinService.RemoveSkin(skin);

            _skinListboxItems.Remove(item);
        }
        private async void Border_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            _preloader.Owner = this;
            _preloader.Show();

            try
            {
                foreach (string file in files)
                {
                    _preloader.SetStatus("Extracting: " + file);

                    await Task.Run(() =>
                    {
                        var skin = _customSkinService.FromFile(file);
                        if (_customSkinService.ImportedSkins.Contains(skin))
                            return;

                        _customSkinService.AddSkin(skin, file);

                        Dispatcher.Invoke(() =>
                            _skinListboxItems.Add(new CustomSkinListBoxItem(skin)));
                    });
                }
            }
            catch (Exception ex)
            {
                new CustomMessageBox("Error!", ex.Message, this).ShowDialog();
            }
            finally
            {
                _preloader.Hide();
            }
        }


        private void Close(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void Search(object sender, TextChangedEventArgs e)
        {
            var q = searchTextBox.Text.ToLowerInvariant();

            _view.Filter = o =>
            {
                var i = (CustomSkinListBoxItem)o;
                return i.RealName.ToLowerInvariant().Contains(q) || i.Text.ToLowerInvariant().Contains(q);
            };
            _view.Refresh();
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border b) return;

            if (b.DataContext is not CustomSkinListBoxItem item) return;

            var skin = _customSkinService.ImportedSkins.FirstOrDefault(x => x.Name == item.RealName);
            if (skin is null) return;

            var newValue = !item.Enabled;
            item.Enabled = newValue;      
            skin.Enabled = newValue;
            _customSkinService.SaveSkins();
        }
    }

    public class CustomSkinListBoxItem : INotifyPropertyChanged
    {
        private bool _enabled;

        public string Name { get; set; } = "";
        public string RealName { get; set; } = "";
        public string Text { get; set; } = "";

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CustomSkinListBoxItem(CustomSkin skin)
        {
            Name = skin.Name.Length > 15 ? skin.Name[..15] + "..." : skin.Name;
            RealName = skin.Name;
            skin.Version = string.IsNullOrEmpty(skin.Version) ? "1.0.0" : skin.Version;
            skin.Author = string.IsNullOrEmpty(skin.Author) ? "unknown" : skin.Author;

            var headerText = $"v{skin.Version} by {skin.Author}";
            headerText = headerText.Length > 30 ? headerText[..30] + "..." : headerText;

            var bottomText = skin.Description.Length > 30 ? skin.Description[..30] + "..." : skin.Description;

            Text = string.IsNullOrEmpty(bottomText) ? headerText : headerText + Environment.NewLine + bottomText;

            Enabled = skin.Enabled;
        }
    }

}
