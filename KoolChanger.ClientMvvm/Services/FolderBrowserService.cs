#region

using System.Windows.Forms;
using KoolChanger.ClientMvvm.Interfaces;

#endregion

namespace KoolChanger.ClientMvvm.Services;

public class FolderBrowserService : IFolderBrowserService
{
    public bool TrySelectFolder(out string? selectedPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where League of Legends is installed",
            UseDescriptionForTitle = true
        };

        var result = dialog.ShowDialog();
        selectedPath = result == DialogResult.OK ? dialog.SelectedPath : null;
        return result == DialogResult.OK && !string.IsNullOrEmpty(selectedPath);
    }
}
