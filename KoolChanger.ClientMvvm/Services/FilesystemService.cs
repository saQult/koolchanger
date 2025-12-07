using System;
using System.IO;
using System.Threading.Tasks;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.Models;

namespace KoolChanger.ClientMvvm.Services;

public class FilesystemService : IFilesystemService
{
    public Task InitializeFoldersAndFilesAsync()
    {
        return Task.Run(() =>
        {
            var folders = new[]
            {
                "installed",
                "profiles",
                "skins",
                Path.Combine("assets", "champions"),
                Path.Combine("assets", "champions", "splashes"),
                Path.Combine(Path.GetTempPath(), "KoolChanger.tmp")
            };

            var files = new[]
            {
                "champion-data.json",
                "customskins.json",
                "config.json"
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }

            foreach (var file in files)
            {
                if (!File.Exists(file))
                    File.Create(file).Dispose();
            }
        });
    }

    public bool IsFirstRun()
    {
        if (!File.Exists("runned"))
        {
            File.Create("runned").Dispose();
            return true;
        }
        return false;
    }

    public bool IsSkinDownloaded(Champion champion, Skin skin)
    {
        if (champion == null || skin == null) return false;

        var champIdStr = champion.Id.ToString();
        var skinIdStr = skin.Id.ToString();

        // Логика вычисления короткого ID скина (как в оригинальной VM)
        if (!skinIdStr.StartsWith(champIdStr)) return false;
        
        var skinIdShort = Convert.ToInt32(skinIdStr.Substring(champIdStr.Length));

        if (skin is SkinForm skinForm)
        {
            var path = Path.Combine("skins", $"{champion.Id}", "special_forms", $"{skinIdShort}", $"{skinForm.Stage}.fantome");
            return File.Exists(path);
        }
        else
        {
            var path = Path.Combine("skins", $"{champion.Id}", $"{skinIdShort}.fantome");
            return File.Exists(path);
        }
    }
}