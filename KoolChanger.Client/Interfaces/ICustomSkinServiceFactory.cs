using KoolChanger.Core.Services;

namespace KoolChanger.Client.Interfaces;

public interface ICustomSkinServiceFactory
{
    CustomSkinService Create(ToolService toolService);
}
