using System;
using KoolChanger.Client.Interfaces;

namespace KoolChanger.Client.Services;

public class LoggingService : ILoggingService
{
    public event Action<string>? OnLog;

    public void Log(string message)
    {
        var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        OnLog?.Invoke(formattedMessage);
    }
}