namespace KoolChanger.Client.Interfaces;

public interface ILoggingService
{
    void Log(string message);
    event Action<string>? OnLog;
}