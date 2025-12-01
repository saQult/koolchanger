using CommunityToolkit.Mvvm.ComponentModel;

namespace KoolChanger.ClientMvvm.ViewModels.Dialogs;

public class PreloaderViewModel : ObservableObject
{
    private string _status;

    public string Status
    {
        get => _status;
        set 
        {
            _status = value;
            OnPropertyChanged();
        }
    }
}