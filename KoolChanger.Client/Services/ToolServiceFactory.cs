using KoolChanger.Client.Interfaces;
using KoolChanger.Core.Services;

namespace KoolChanger.Client.Services;

public class ToolServiceFactory : IToolServiceFactory
{
    public ToolService Create(string gamePath)
    {
        return new ToolService(gamePath);
    }
}
