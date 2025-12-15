using KoolChanger.Core.Services;

namespace KoolChanger.Client.Interfaces;

public interface IToolServiceFactory
{
    ToolService Create(string gamePath);
}
