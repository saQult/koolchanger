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
        string repoName = "lol-skins";
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
                    await ConvertToDev(targetDir);
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

    //thank you darkseal.org for removing dev repo from public I hope u will be safe :D
    public async Task ConvertToDev(string directory)
    {
        Sanitizer.SanitizeTree(directory);

        var champions = await _championService.GetChampionsAsync();
        champions = await _skinService.GetAllSkinsAsync(champions);

        foreach (var champion in champions)
        {
            var oldDirectory = Path.Combine(directory, NameRules.SanitizeForPath(champion.Name));
            var newDirectory = Path.Combine(directory, champion.Id.ToString());

            if (Directory.Exists(oldDirectory) && !string.Equals(oldDirectory, newDirectory, StringComparison.OrdinalIgnoreCase))
                Directory.Move(oldDirectory, newDirectory);

            foreach (var skin in champion.Skins.Skip(1))
            {
                var skinBaseName = NameRules.SanitizeForPath(skin.Name);
                var oldFile = Path.Combine(newDirectory, skinBaseName + ".zip");

                var skinId = Convert.ToInt32(
                    skin.Id.ToString().Substring(
                        champion.Id.ToString().Length,
                        skin.Id.ToString().Length - champion.Id.ToString().Length));

                var newFileZip = Path.Combine(newDirectory, $"{skinId}.zip");
                var newFileFantome = Path.ChangeExtension(newFileZip, ".fantome");

                foreach (var chroma in skin.Chromas)
                {
                    var chromaFolderName = NameRules.SanitizeForPath(chroma.Name);

                    var chromaPath = Path.Combine(newDirectory, "chromas", chromaFolderName);
                    if (Directory.Exists(chromaPath) == false) continue;

                    var file = Directory.GetFiles(chromaPath).FirstOrDefault(x => x.Contains(chroma.Id.ToString()));

                    if (File.Exists(file) == false) continue;


                    skinId = Convert.ToInt32(
                        chroma.Id.ToString().Substring(
                            champion.Id.ToString().Length,
                            chroma.Id.ToString().Length - champion.Id.ToString().Length));

                    var newChromaZip = Path.Combine(newDirectory, $"{skinId}.zip");
                    var newChromaFantome = Path.ChangeExtension(newChromaZip, ".fantome");

                    if (!File.Exists(newChromaFantome))
                        File.Copy(file, newChromaFantome);
                }

                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFileFantome))
                        File.Delete(newFileFantome);

                    File.Move(oldFile, newFileFantome);
                }
            }
        }
    }


    // made by random guy from lolru discord
    private static class NameRules
    {
        private static readonly Regex RX_WIN_FORBIDDEN = new(@"[\\/:*?""<>|]+", RegexOptions.Compiled);
        private static readonly Regex RX_NON_WORD = new(@"[^A-Za-z0-9_\-\s]+", RegexOptions.Compiled);
        private static readonly Regex RX_SPACES = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RX_TRIM_EDGES = new(@"^[_\-]+|[_\-]+$", RegexOptions.Compiled);

        public static string SanitizeForPath(string name, string fallback = "item")
        {
            if (string.IsNullOrWhiteSpace(name)) return fallback;

            var s = RX_WIN_FORBIDDEN.Replace(name, " ");
            s = RX_NON_WORD.Replace(s, " ");
            s = RX_SPACES.Replace(s, "_");
            s = RX_TRIM_EDGES.Replace(s, "");

            if (string.IsNullOrEmpty(s)) s = fallback;

            return s;
        }
    }

}
