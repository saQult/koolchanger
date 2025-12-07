using CSLOLTool.Models;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace CSLOLTool.Services;
public class UpdateService
{
    public event Action<string>? OnUpdating;
    public ChampionService _championService = new();
    public SkinService _skinService = new();
    public async Task DownloadSkins()
    {
        string repoName = "LeagueSkins";
        string branch = "main";
        string zipUrl = $"https://github.com/Alban1911/{repoName}/archive/refs/heads/{branch}.zip";

        string tempDir = Path.Combine(Path.GetTempPath(), $"lolskins-{Guid.NewGuid()}");
        string zipPath = Path.Combine(tempDir, "repo.zip");
        string extractPath = Path.Combine(tempDir, "extracted");
        string targetDir = Path.Combine(Directory.GetCurrentDirectory(), "skins");
        try
        {
            Directory.CreateDirectory(tempDir);
            OnUpdating?.Invoke("Downloading skins repo...");

            using (HttpClient client = new HttpClient())
            using (var response = await client.GetAsync(zipUrl))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                await response.Content.CopyToAsync(fs);
            }

            OnUpdating?.Invoke("Unzipping skins repo...");
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            string extractedRepoPath = Path.Combine(extractPath, $"{repoName}-{branch}", "skins");

            if (!Directory.Exists(extractedRepoPath))
            {
                OnUpdating?.Invoke("Extracted repo folder not found");
                return;
            }

            OnUpdating?.Invoke("Copying champion folders...");
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);

            Directory.CreateDirectory(targetDir);

            foreach (string dir in Directory.GetDirectories(extractedRepoPath))
            {
                string folderName = Path.GetFileName(dir);
                string dest = Path.Combine(targetDir, folderName);
                CopyDirectory(dir, dest);
            }

            OnUpdating?.Invoke("Finished copying folders");
        }
        catch (Exception ex)
        {
            OnUpdating?.Invoke("Error: " + ex.Message);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                if (Directory.Exists(targetDir))
                    ChangeStructure(targetDir);
            }
            catch { }
        }
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
    private static void ChangeStructure(string dir)
    {
        var directories = Directory.GetDirectories(dir);
        foreach (var championDirectory in directories) 
        {
            var skinDirectories = Directory.GetDirectories(championDirectory);
            foreach (var skinDirectory in skinDirectories)
            {
                var skinFiles = Directory.GetFiles(skinDirectory);
                foreach (var skinFile in skinFiles)
                {
                    try 
                    {
                        var skinName = skinFile.Split("\\")[skinFile.Split("\\").Length - 1].Replace("zip", "fantome");
                        File.Copy(skinFile, Path.Combine(championDirectory, skinName));
                    } catch { }
                }
                var chromaDirectories = Directory.GetDirectories(skinDirectory);
                foreach (var chromaDirectory in chromaDirectories)
                {
                    var chromaFiles = Directory.GetFiles(chromaDirectory);
                    foreach (var chromaFile in chromaFiles)
                    {
                        try
                        {
                            var chromaName = chromaFile.Split("\\")[chromaFile.Split("\\").Length - 1].Replace("zip", "fantome"); ;
                            File.Copy(chromaFile, Path.Combine(championDirectory, chromaName));
                        }
                        catch { }
                    }
                }
                Directory.Delete(skinDirectory, true);
            }
        }
    }

}
