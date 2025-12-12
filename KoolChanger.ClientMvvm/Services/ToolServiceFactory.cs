#region

using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.Services;

#endregion

namespace KoolChanger.ClientMvvm.Services;

public class ToolServiceFactory : IToolServiceFactory
{
    public ToolService Create(string gamePath)
    {
        return new ToolService(gamePath);
    }
}
