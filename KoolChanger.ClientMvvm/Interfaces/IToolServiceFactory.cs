using KoolChanger.Services;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface IToolServiceFactory
{
    ToolService Create(string gamePath);
}
