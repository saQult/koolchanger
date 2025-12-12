using KoolChanger.Services;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface ICustomSkinServiceFactory
{
    CustomSkinService Create(ToolService toolService);
}
