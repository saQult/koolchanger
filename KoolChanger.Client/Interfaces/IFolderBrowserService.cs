namespace KoolChanger.Client.Interfaces;

public interface IFolderBrowserService
{
    bool TrySelectFolder(out string? selectedPath);
}
