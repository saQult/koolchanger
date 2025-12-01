#region

using System.Windows;
using System.Windows.Media.Effects;
using KoolChanger.Helpers;

#endregion

namespace KoolChanger.ClientMvvm.Views.Dialogs;

public partial class CustomMessageBox : Window
{
    public CustomMessageBox()
    {
        InitializeComponent();

        Loaded += CustomMessageBox_Loaded;
        Closed += CustomMessageBox_Closed;
    }

    private void CustomMessageBox_Loaded(object sender, RoutedEventArgs e)
    {
        var windowBlurEffect = new WindowBlurEffect(this, AccentState.ACCENT_ENABLE_BLURBEHIND)
        {
            BlurOpacity = 100
        };

        if (Owner != null) Owner.Effect = new BlurEffect { Radius = 10 };
    }

    private void CustomMessageBox_Closed(object sender, EventArgs e)
    {
        if (Owner != null) Owner.Effect = null;
    }
}