using KoolChanger.Client.Models;
using KoolChanger.Client.ViewModels;
using KoolChanger.Core.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace KoolChanger.Client.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
            Loaded += (_, e) => new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND) { BlurOpacity = 10 };
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
            Application.Current.Shutdown();
        }
        private void TreeViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is NavItem navItem)
            {
                navItem.NavigateCommand?.Execute(navItem);
                e.Handled = true;
            }
        }

        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (WindowState != WindowState.Normal) return;
            var newWidth = Width + e.HorizontalChange;
            if (newWidth >= MinWidth) Width = newWidth;
        }

        private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (WindowState != WindowState.Normal) return;
            var newWidth = Width - e.HorizontalChange;
            if (newWidth >= MinWidth)
            {
                Left += e.HorizontalChange;
                Width = newWidth;
            }
        }

        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (WindowState != WindowState.Normal) return;
            var newHeight = Height + e.VerticalChange;
            if (newHeight >= MinHeight) Height = newHeight;
        }

        private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (WindowState != WindowState.Normal) return;
            var newHeight = Height - e.VerticalChange;
            if (newHeight >= MinHeight)
            {
                Top += e.VerticalChange;
                Height = newHeight;
            }
        }

        private void ResizeTopLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTop_DragDelta(sender, e);
            ResizeLeft_DragDelta(sender, e);
        }

        private void ResizeTopRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTop_DragDelta(sender, e);
            ResizeRight_DragDelta(sender, e);
        }

        private void ResizeBottomLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom_DragDelta(sender, e);
            ResizeLeft_DragDelta(sender, e);
        }

        private void ResizeBottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeBottom_DragDelta(sender, e);
            ResizeRight_DragDelta(sender, e);
        }
    }
}
