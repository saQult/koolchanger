#region

using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.Services;

#endregion

namespace KoolChanger.ClientMvvm.Services;

public class CustomSkinServiceFactory : ICustomSkinServiceFactory
{
    public CustomSkinService Create(ToolService toolService)
    {
        return new CustomSkinService(toolService);
    }
}
