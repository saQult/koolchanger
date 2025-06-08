using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace CSLOLTool;
public class Tool
{
    private readonly string _programPath;
    private string _gamePath;

    public event Action<string>? StatusChanged;
    public event Action<string, string, string>? ErrorReported;

    private record ModInfo(string Author, string Name, string Description, string Version);

    public Tool(string gamePath)
    {
        _programPath = AppContext.BaseDirectory;
        _gamePath = MakePath(gamePath);
        Log("Version: 1.0.0\n");
    }

    private string MakePath(string path) => "\"" + path + "\"";

    private static ModInfo? GetModInfoFromZip(string zipPath)
    {
        if (!File.Exists(zipPath))
            return null;

        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.GetEntry("META/info.json");

        if (entry == null)
            return null;

        using var reader = new StreamReader(entry.Open());
        var json = reader.ReadToEnd();

        var modInfo = JsonSerializer.Deserialize<ModInfo>(json);
        return modInfo;
    }

    private void Log(string message)
    {
        try
        {
            var logPath = Path.Combine(_programPath, "log.txt");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
        catch { }
    }

    public void SetStatus(string status)
    {
        Log(status + "\n");
        StatusChanged?.Invoke(status);
    }

    public void SetLeaguePath(string path)
    {
        if (File.Exists(Path.Combine(path, "League of Legends.exe")))
        {
            _gamePath = Path.GetFullPath(path);
        }
    }

    public void Import(string src)
    {
        if (File.Exists(src) == false)
            return;
        var modInfo = GetModInfoFromZip(src);
        if (modInfo == null)
            return;
        var path = Path.Combine(_programPath, "installed", modInfo.Name);
        var args = new List<string>
        {
            "import",
            MakePath(src),
            MakePath(path),
            "--game:" + _gamePath,
            "--noTFT"
        };

        RunTool(args, true, (exitCode, proc) =>
        {
            StatusChanged?.Invoke("Import wad finished with code: " + exitCode);
        });
    }

    public void SaveOverlay(string profileName, IEnumerable<string> mods, bool skipConflicts)
    {
        var args = new List<string>
        {
            "mkoverlay",
            MakePath(Path.Combine(_programPath, "installed")),
            MakePath(Path.Combine(_programPath, "profiles", profileName)),
            "--game:" + _gamePath,
            "--mods:" + MakePath(string.Join('/', mods))
        };
        if (skipConflicts) args.Add("--ignoreConflict");
        RunTool(args, false, (code, proc) =>
        {
            SetStatus("Overlay created with code: " + code);
        });
        File.WriteAllText(Path.Combine(_programPath, "profiles", profileName) + $"\\{profileName}.config", mods.Count().ToString());
    }

    public Process RunOverlay(string profileName)
    {
        var args = new List<string>
        {
            "runoverlay",
            Path.Combine(_programPath, "profiles", profileName),
            Path.Combine(_programPath, "profiles", profileName + ".config"),
            "--game:" + _gamePath,
        };

        return RunTool(args, false, (code, proc) =>
        {
            SetStatus("Overlay run finished with code: " + code);
        });
    }

    private Process RunTool(List<string> args, bool waitForFinish, Action<int, Process> onFinish)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_programPath, "csloltools", "mod-tools.exe"),
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Log("[stdout] " + e.Data + "\n");
                StatusChanged?.Invoke(e.Data.Trim());
            }
        };

        process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Log("[stderr] " + e.Data + "\n");
                StatusChanged?.Invoke(e.Data.Trim());
            }
        };

        process.Exited += (s, e) =>
        {
            Log("Process exited with code: " + process.ExitCode + "\n");
            onFinish?.Invoke(process.ExitCode, process);
            process.Dispose();
        };

        try
        {
            Log("Starting process: " + process.StartInfo.FileName + " " + process.StartInfo.Arguments + "\n");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (waitForFinish)
                process.WaitForExit();
        }
        catch (Exception ex)
        {
            Log("Process error: " + ex.Message + "\n");
            StatusChanged?.Invoke(ex.Message);
            onFinish?.Invoke(-1, process);
        }

        return process;
    }
}
