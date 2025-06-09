using CSLOLTool.Models;
using System.IO.Compression;

namespace CSLOLTool.Services;
public class UpdateService
{
    public event Action<string>? OnUpdating;
    public async Task DownloadSkins()
    {
        string repoName = "lol-skins-developer";
        string branch = "main";
        string zipUrl = $"https://github.com/darkseal-org/{repoName}/archive/refs/heads/{branch}.zip";

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

            string extractedRepoPath = Path.Combine(extractPath, $"{repoName}-{branch}");

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
            }
            catch { }
        }
    }

    static void CopyDirectory(string sourceDir, string targetDir)
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
}
