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
        if (champion == null || skin == null) 
            return false;

        var champIdStr = champion.Id.ToString();
        var skinIdStr = skin.Id.ToString();

        // Проверка что skin.Id начинается с champion.Id
        if (!skinIdStr.StartsWith(champIdStr))
            return false;

        // skinIdShort = XX в skinXX.zip
        if (!int.TryParse(skinIdStr.Substring(champIdStr.Length), out int skinIdShort))
            return false;

        var championFolder = Path.Combine("Skins", champion.Name);

        string skinFile = $"skin{skinIdShort}.zip";

        var finalPath = Path.Combine(championFolder, skinFile);

        return File.Exists(finalPath);
    }
}