using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using ManagedWrapper;
namespace KoolChanger;
public class Tool
{
    
    private string _gamePath;
    private ModTool _modTool;
    public event Action<string>? StatusChanged;
    private readonly SemaphoreSlim _runOverlayLock = new(1, 1); // Позволяет только 1 потоку
    private Task? _currentRunOverlayTask;
    private record ModInfo(string Author, string Name, string Description, string Version);

    public Tool(string gamePath, ModTool modTool)
    {
        _modTool = modTool;
        _gamePath = gamePath;
        Log("Version: 1.0.0\n");
    }


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
            var logPath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
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
        else
        {
            StatusChanged?.Invoke($"League of Legends not found at {path}");
        }
    }

    public void Import(string src, string name)
    {
        if (File.Exists(src) == false)
            return;
        var modInfo = GetModInfoFromZip(src);
        if (modInfo == null)
            return;
        var path = Path.Combine(Directory.GetCurrentDirectory(), "installed", name);
        _modTool.Import( src, path, _gamePath, true);
    }

    public void SaveOverlay(string profileName, IEnumerable<string> mods, bool skipConflicts)
    {
        _modTool.MkOverlay( Path.Combine(Directory.GetCurrentDirectory(), "installed"),
                            (Path.Combine(Directory.GetCurrentDirectory(), "profiles", profileName)),
                                _gamePath, 
                            (string.Join('/', mods)),
                            true,
                            skipConflicts);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "profiles", profileName) + $"\\{profileName}.config", mods.Count().ToString());
    }

   public Task RunOverlay(string profileName)
    {
        // 1. Проверяем, запущен ли предыдущий
        if (_currentRunOverlayTask != null && !_currentRunOverlayTask.IsCompleted)
        {
            SetStatus("Stopping previous overlay instance...");
            
            // 2. Отменяем предыдущий вызов через ModTool::Cancel()
            try 
            {
                _modTool.Cancel(); 
            }
            catch (Exception ex)
            {
                Log($"Error stopping previous overlay: {ex.Message}");
            }

            // Ожидаем завершения предыдущей задачи
            try
            {
                // Ждем некоторое время, чтобы C++ код успел корректно завершиться 
                // и освободить ресурсы. 
                _currentRunOverlayTask.Wait(TimeSpan.FromSeconds(5)); 
            }
            catch (Exception)
            {
                // Игнорируем исключения ожидания (например, OperationCanceledException),
                // так как мы ожидаем, что задача будет отменена.
            }

            // Сбрасываем ссылку
            _currentRunOverlayTask = null;
        }

        // 3. Запускаем новую задачу
        var newRunTask = Task.Run(() => 
        {
            try
            {
                SetStatus($"Starting overlay for profile: {profileName}");

                _modTool.RunOverlay(
                    Path.Combine(Directory.GetCurrentDirectory(), "profiles", profileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "profiles", profileName + ".config"),
                    _gamePath,
                    "");
                
                Console.WriteLine("Overlay finished.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log(e.Message);
                SetStatus($"Overlay error: {e.Message}");
            }
            finally
            {
                // Если эта задача завершилась, и это текущая активная задача, сбросить ее.
                // Это важно, чтобы избежать гонки, если новый вызов начался, пока этот завершался.
                if (_currentRunOverlayTask.Id == Task.CurrentId) 
                {
                     _currentRunOverlayTask = null;
                }
            }
        });
        
        // 4. Сохраняем ссылку на новую задачу
        _currentRunOverlayTask = newRunTask;

        return newRunTask;
    }
    public void Stop()
    {
        if (_currentRunOverlayTask != null && !_currentRunOverlayTask.IsCompleted)
        {
            try 
            {
                _modTool.Cancel();
                _currentRunOverlayTask.Wait(TimeSpan.FromSeconds(5)); 
            }
            catch (Exception ex)
            {
                Log($"Error stopping overlay: {ex.Message}");
            }
            finally
            {
                _currentRunOverlayTask = null;
            }
        }
    }
}
