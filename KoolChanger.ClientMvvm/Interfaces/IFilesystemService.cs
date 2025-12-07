using KoolChanger.Models;
using System.Threading.Tasks;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface IFilesystemService
{
    Task InitializeFoldersAndFilesAsync();
    bool IsFirstRun();
    bool IsSkinDownloaded(Champion champion, Skin skin);
}