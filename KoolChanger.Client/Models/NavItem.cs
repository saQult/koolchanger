using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace KoolChanger.Client.Models
{
    /// <summary>
    /// Main navigation sidebar model
    /// </summary>
    public partial class NavItem : ObservableObject
    {
        public string Title { get; }
        public string? Key { get; }
        public ObservableCollection<NavItem> Children { get; } = new();

        [ObservableProperty] private bool isExpanded = true;
        [ObservableProperty] private bool isSelected;

        public bool IsLeaf => Children.Count == 0 && !string.IsNullOrWhiteSpace(Key);

        public RelayCommand<NavItem>? NavigateCommand { get; set; }

        public NavItem(string title, string? key = null, params NavItem[] children)
        {
            Title = title;
            Key = key;

            foreach (var c in children)
                Children.Add(c);
        }
    }
}
