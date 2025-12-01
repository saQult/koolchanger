#region

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.ClientMvvm.Services;

#endregion

namespace KoolChanger.ClientMvvm.ViewModels;

public class CustomMessageBoxViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    public CustomMessageBoxViewModel(string header, string message, INavigationService navigationService)
    {
        HeaderText = header;
        MessageText = message;
        _navigationService = navigationService;

        YesCommand = new RelayCommand(() => OnYesExecuted());
        NoCommand = new RelayCommand(() => OnNoExecuted());
    }

    public bool? DialogResult { get; private set; }

    public string HeaderText { get; }
    public string MessageText { get; }
    
    public RelayCommand YesCommand { get; }
    public RelayCommand NoCommand { get; }

    private void OnYesExecuted()
    {
        DialogResult = true;
        _navigationService.CloseWindow(this);
    }

    private void OnNoExecuted()
    {
        DialogResult = false;
        _navigationService.CloseWindow(this);
    }
}