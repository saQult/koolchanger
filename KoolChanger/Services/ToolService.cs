using System.Diagnostics;
using static KoolChanger.Services.ToolService;

namespace KoolChanger.Services;

public class ToolService
{
    public event Action<string>? OverlayRunned;

    private Tool _tool;

    public ToolService(string gamePath)
    {
        _tool = new Tool(gamePath);
        _tool.StatusChanged += (data) => OverlayRunned?.Invoke(data);
        if (Directory.Exists("skins") == false)
            return;

    }
    public Process Run(IEnumerable<string> mods)
    {
        _tool.SaveOverlay("default", mods, false);
        return _tool.RunOverlay("default");
    }
    public void Import(string path, string name) => _tool.Import(path, name);
   
}
