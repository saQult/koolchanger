using KoolChanger.Core.Models;
using System.Threading.Tasks;

namespace KoolChanger.Client.Interfaces;

public interface IFilesystemService
{
    Task InitializeFoldersAndFilesAsync();
    bool IsFirstRun();
    bool IsSkinDownloaded(Champion champion, Skin skin);
}