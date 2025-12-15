using KoolWrapper;

namespace KoolChanger.Core.Services;

public class ToolService : IDisposable 
{
    public event Action<string>? OverlayRunned;

    private readonly Tool _tool;
    private readonly ModTool _modTool;
    
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    
    private CancellationTokenSource? _activeCts;
    
    private Task? _activeTask;

    public bool IsRunning => _executionLock.CurrentCount == 0; 
    
    public ToolService(string gamePath)
    {
        _modTool = new ModTool();
        _tool = new Tool(gamePath, _modTool);
        _tool.StatusChanged += (data) => OverlayRunned?.Invoke(data);
        if (Directory.Exists("skins") == false)
            return;
    }

    public async Task Run(IEnumerable<string> mods)
    {
        await _executionLock.WaitAsync();

        try
        {
            if (_activeCts != null)
            {
                _activeCts.Cancel();
                _activeCts.Dispose();
                _activeCts = null;
            }

            if (_activeTask != null && !_activeTask.IsCompleted)
            {
                try
                {
                    await _activeTask;
                }
                catch (OperationCanceledException) 
                { 
                }
                catch (Exception ex)
                {
                    OverlayRunned?.Invoke($"Previous run failed: {ex.Message}");
                }
            }

            _activeCts = new CancellationTokenSource();
            var token = _activeCts.Token;

            _activeTask = RunInternal(mods, token);
        }
        finally
        {
            _executionLock.Release();
        }

        if (_activeTask != null) await _activeTask;
    }

    private async Task RunInternal(IEnumerable<string> mods, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            await Task.Run(() => 
            {
                _tool.SaveOverlay("default", mods, true); 
            }, token); 

            using (token.Register(() => _tool.Stop())) 
            {
                token.ThrowIfCancellationRequested();
            
                await Task.Run(() => 
                {
                    _tool.RunOverlay("default");
                }, token);
            }
        }
        catch (OperationCanceledException)
        {
            OverlayRunned?.Invoke("Операция отменена.");
        }
        catch (Exception ex)
        {
            OverlayRunned?.Invoke($"Ошибка запуска: {ex.Message}");
        }
    }
    
    public void Cancel()
    {
        _activeCts?.Cancel();
    }

    public void Import(string path, string name) => _tool.Import(path, name);
    
    public void Dispose()
    {
        Cancel(); 
        _executionLock.Dispose();
    }
}