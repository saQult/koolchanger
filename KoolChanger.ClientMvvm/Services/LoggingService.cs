using System;
using KoolChanger.ClientMvvm.Interfaces;

namespace KoolChanger.ClientMvvm.Services;

public class LoggingService : ILoggingService
{
    public event Action<string>? OnLog;

    public void Log(string message)
    {
        var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        OnLog?.Invoke(formattedMessage);
    }
}