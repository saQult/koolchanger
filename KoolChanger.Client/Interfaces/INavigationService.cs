using KoolChanger.Client.ViewModels;
using KoolChanger.Client.ViewModels.Dialogs;
using KoolChanger.Core.Services;
using System.ComponentModel;

namespace KoolChanger.Client.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    void Register<TViewModel, TView>() 
        where TViewModel : class 
        where TView : System.Windows.Window;

    void NavigateTo<TViewModel>() where TViewModel : class;

    void ShowDialog<TViewModel>() where TViewModel : class;

    void ShowSettings();
    void ShowCustomSkins(Core.Services.CustomSkinService? customSkinService = null); 
    CustomMessageBoxViewModel ShowCustomMessageBox(string header, string message);
    
    void CloseWindow(object viewModel);

    void RegisterPage<TViewModel, TPage>()
        where TViewModel : class
        where TPage : System.Windows.Controls.Page;
    void NavigateToPage<TViewModel>() where TViewModel : class;
    System.Windows.Controls.Page? CurrentPage { get; }
}