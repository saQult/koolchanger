using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.Services;

namespace KoolChanger.ClientMvvm.Services;

public class ToolServiceFactory : IToolServiceFactory
{
    public ToolService Create(string gamePath)
    {
        return new ToolService(gamePath);
    }
}
