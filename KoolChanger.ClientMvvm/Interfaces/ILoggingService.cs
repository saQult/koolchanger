using System;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface ILoggingService
{
    void Log(string message);
    event Action<string>? OnLog; // Для подписки MainViewModel на обновления
}