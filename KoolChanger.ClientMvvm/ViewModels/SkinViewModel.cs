using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoolChanger.Models;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace KoolChanger.ClientMvvm.ViewModels;

public class SkinViewModel : ObservableObject
{
    private bool _isSelected;
    private SkinViewModel? _chromaPreview;
    
    public SkinViewModel()
    {
        ShowChromaPreviewCommand = new RelayCommand<SkinViewModel>(ShowChromaPreview);
        HideChromaPreviewCommand = new RelayCommand(HideChromaPreview);
    }
    
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public bool IsChroma { get; set; }
    public bool IsForm { get; set; }
    public bool IsChromaVisible { get; set; } = false;
    public Skin Model { get; set; } = null!; 
    public Champion Champion { get; set; } = null!;
    public SkinViewModel? Parent { get; set; }

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

    public ICommand ShowChromaPreviewCommand { get; set; }
    public ICommand HideChromaPreviewCommand { get; set; }

    private void ShowChromaPreview(SkinViewModel? chroma)
    {
        if (Parent == null || chroma == null) return;
        Parent.ChromaPreview = chroma;
        Parent.ChromaPreview.IsChromaVisible = false;
    }

    private void HideChromaPreview()
    {
        if (Parent == null) return;
        Parent.ChromaPreview.IsChromaVisible = true;
        Parent.ChromaPreview = null;
    }
    public ObservableCollection<SkinViewModel> Children { get; set; } = new();
}