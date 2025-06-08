using System.Diagnostics;
using static CSLOLTool.Services.ToolService;

namespace CSLOLTool.Services;

public class ToolService
{
    public event Action<string>? OverlayRunned;
    public event Action<string>? ChromaInstalled;
    public event Action<string>? SkinInstalled;

    private Tool _tool;

    public int InstallSkinCount { get; private set; }
    public int InstallChromaCount { get; private set; }

    private int _installedSkins;
    public int InstalledSkins => _installedSkins;

    private int _installedChromas;
    public int InstalledChromas => _installedChromas;

    public ToolService(string gamePath)
    {
        _tool = new Tool(gamePath);

        if (Directory.Exists("skins") == false)
            return;

        InstallSkinCount = Directory.GetDirectories("skins")
            .SelectMany(Directory.GetFiles)
            .Count();
        InstallChromaCount = Directory.GetDirectories("skins")
            .SelectMany(championDir => Directory.GetDirectories(Path.Combine(championDir, "chromas")))
            .SelectMany(chromaDir => Directory.GetFiles(chromaDir, "*.zip", SearchOption.TopDirectoryOnly))
            .Count();
    }
    public Process Run(IEnumerable<string> mods)
    {
        _tool.StatusChanged += (data) => OverlayRunned?.Invoke(data);
        _tool.SaveOverlay("default", mods, false);
        return _tool.RunOverlay("default");
    }
    public void Import(string path) => _tool.Import(path);
    public void LoadBasicSkins(int maxParallelism = 10)
    {
        var files = Directory.GetDirectories("skins")
            .SelectMany(Directory.GetFiles)
            .ToList();

        InstallSkinCount = Directory.GetDirectories("skins")
            .SelectMany(Directory.GetFiles)
            .Count();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism
        };

        Parallel.ForEach(files, options, (Action<string>)(file =>
        {
            SkinInstalled?.Invoke(file);
            _tool.Import(file);
            Interlocked.Increment(ref _installedSkins);
        }));

        _installedSkins = 0;
    }
    public void LoadChromas(int maxParallelism = 10)
    {
        var chromaFiles = Directory.GetDirectories("skins")
            .SelectMany(championDir => Directory.GetDirectories(Path.Combine(championDir, "chromas")))
            .SelectMany(chromaDir => Directory.GetFiles(chromaDir, "*.zip", SearchOption.TopDirectoryOnly))
            .ToList();

        InstallChromaCount = Directory.GetDirectories("skins")
            .SelectMany(championDir => Directory.GetDirectories(Path.Combine(championDir, "chromas")))
            .SelectMany(chromaDir => Directory.GetFiles(chromaDir, "*.zip", SearchOption.TopDirectoryOnly))
            .Count();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxParallelism
        };

        Parallel.ForEach(chromaFiles, options, (Action<string>)(file =>
        {
            ChromaInstalled?.Invoke(file);
            _tool.Import(file);
            Interlocked.Increment(ref _installedChromas);
        }));

        _installedChromas = 0;
    }

}
