namespace KoolChanger.ClientMvvm.Interfaces;

public interface IFolderBrowserService
{
    bool TrySelectFolder(out string? selectedPath);
}
