#region

using KoolChanger.Client.Interfaces;
using KoolChanger.Core.Services;

#endregion

namespace KoolChanger.Client.Services;

public class CustomSkinServiceFactory : ICustomSkinServiceFactory
{
    public CustomSkinService Create(ToolService toolService)
    {
        return new CustomSkinService(toolService);
    }
}
