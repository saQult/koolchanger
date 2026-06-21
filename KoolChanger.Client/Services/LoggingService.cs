using System;
using System.IO;
using KoolChanger.Client.Interfaces;

namespace KoolChanger.Client.Services;

public class LoggingService : ILoggingService
{
    private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "koolchanger.log");

    public event Action<string>? OnLog;

    public void Log(string message)
    {
        var formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        OnLog?.Invoke(formattedMessage);
        try { File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine); } catch { }
    }
}