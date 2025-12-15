using System.Collections.Generic;
using KoolChanger.Core.Models;
using KoolChanger.Core.Services;

namespace KoolChanger.Client.Interfaces;

public interface IPartyServiceFactory
{
    PartyService Create(List<Champion> champions, Dictionary<Champion, Skin> selectedSkins, string partyServerUrl);
}
