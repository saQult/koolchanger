using CommunityToolkit.Mvvm.ComponentModel;

namespace KoolChanger.Client.ViewModels.Dialogs;

public class PreloaderViewModel : ObservableObject
{
    private bool _isActive;
    private string? _status;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public string? Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }
}