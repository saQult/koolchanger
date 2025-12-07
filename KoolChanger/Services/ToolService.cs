// ToolService.cs

using ManagedWrapper;
// using static KoolChanger.Services.ToolService; // Убрать, если это ошибка компиляции

namespace KoolChanger.Services;

public class ToolService : IDisposable // Добавить IDisposable для корректной очистки
{
    public event Action<string>? OverlayRunned;

    private readonly Tool _tool;
    private readonly ModTool _modTool;
    
    // 1. Lock для предотвращения множественного запуска и атомарности логики отмены/запуска
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    
    // 2. Источник токена отмены для управления текущим процессом
    private CancellationTokenSource? _activeCts;
    
    // 3. Ссылка на активную задачу
    private Task? _activeTask;

    // Свойство для проверки состояния (полезно для UI, даже если это ядро)
    public bool IsRunning => _executionLock.CurrentCount == 0; 
    
    public ToolService(string gamePath)
    {
        _modTool = new ModTool();
        _tool = new Tool(gamePath, _modTool);
        _tool.StatusChanged += (data) => OverlayRunned?.Invoke(data);
        if (Directory.Exists("skins") == false)
            return;
    }

    // Обновленный метод Run. Он реализует логику "Отмена и Перезапуск".
    public async Task Run(IEnumerable<string> mods)
    {
        // 1. Захватываем лок. Гарантирует, что только один поток/вызов Run может менять состояние.
        await _executionLock.WaitAsync();

        try
        {
            // 2. Отменяем предыдущую задачу (если она есть)
            if (_activeCts != null)
            {
                _activeCts.Cancel();
                _activeCts.Dispose();
                _activeCts = null;
            }

            // 3. Ждем, пока старый C++ поток завершит работу (критично!)
            if (_activeTask != null && !_activeTask.IsCompleted)
            {
                try
                {
                    // Ожидаем, что старая задача завершится после сигнала отмены
                    await _activeTask;
                }
                catch (OperationCanceledException) 
                { 
                    // Это ожидаемо и нормально, так как мы сами отменили
                }
                catch (Exception ex)
                {
                    // Логируем, если старая задача завершилась ошибкой, но продолжаем
                    OverlayRunned?.Invoke($"Previous run failed: {ex.Message}");
                }
            }

            // 4. Запускаем новую задачу
            _activeCts = new CancellationTokenSource();
            var token = _activeCts.Token;

            // Сохраняем и запускаем новую задачу.
            _activeTask = RunInternal(mods, token);
        }
        finally
        {
            // 5. Отпускаем замок, разрешая новому вызову Run войти.
            _executionLock.Release();
        }

        // Ожидаем завершения текущей задачи (если вызывающий поток ждет).
        if (_activeTask != null) await _activeTask;
    }

    // Внутренняя логика выполнения с поддержкой отмены
    private async Task RunInternal(IEnumerable<string> mods, CancellationToken token)
    {
        try
        {
            // Шаг 1: Сборка Оверлея (Уже в Task.Run)
            token.ThrowIfCancellationRequested();
            await Task.Run(() => 
            {
                _tool.SaveOverlay("default", mods, true); 
            }, token); 

            // Шаг 2: Запуск Оверлея (долгое C++ ожидание) - НУЖНО ОБЕРНУТЬ В Task.Run
            using (token.Register(() => _tool.Stop())) 
            {
                token.ThrowIfCancellationRequested();
            
                // !!! ИСПРАВЛЕНИЕ: Оборачиваем блокирующий вызов в Task.Run
                await Task.Run(() => 
                {
                    _tool.RunOverlay("default");
                }, token); // Task.Run принимает токен
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
    
    // Метод для вызова отмены извне (например, из UI)
    public void Cancel()
    {
        _activeCts?.Cancel();
    }

    public void Import(string path, string name) => _tool.Import(path, name);
    
    public void Dispose()
    {
        // При уничтожении сервиса отменяем все активные задачи
        Cancel(); 
        _executionLock.Dispose();
        
        // C++ обертка ModTool также должна быть очищена, если это необходимо:
        // ((IDisposable)_modTool).Dispose(); // Добавьте IDisposable к ModTool, если это не сделано
    }
}