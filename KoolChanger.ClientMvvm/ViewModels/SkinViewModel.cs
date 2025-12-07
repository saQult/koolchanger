using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.Models;

namespace KoolChanger.ClientMvvm.ViewModels;

public class SkinViewModel : ObservableObject
{
    private bool _isSelected;
    private SkinViewModel? _chromaPreview;
    
    // Fix: Commands are now initialized with dummy commands 
    // to prevent null errors if they are not dynamically set in MainViewModel
    public SkinViewModel()
    {
        // Dummy commands that do nothing, but are safe to bind to.
        ShowChromaPreviewCommand = new RelayCommand<SkinViewModel>(_ => { });
        HideChromaPreviewCommand = new RelayCommand(() => { });
    }
    
    // Properties
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public bool IsChroma { get; set; }
    public bool IsForm { get; set; }

    public Skin Model { get; set; } = null!; 
    public Champion Champion { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public SkinViewModel? ChromaPreview
    {
        get => _chromaPreview;
        set => SetProperty(ref _chromaPreview, value);
    }

    // Fix: ICommand properties should be read-only if not set via constructor/DI
    // The MainViewModel will still assign them dynamically for the preview logic.
    public ICommand ShowChromaPreviewCommand { get; set; }
    public ICommand HideChromaPreviewCommand { get; set; }

    public ObservableCollection<SkinViewModel> Children { get; set; } = new();
}