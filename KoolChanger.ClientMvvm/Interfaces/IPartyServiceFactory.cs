using System.Collections.Generic;
using KoolChanger.Models;
using KoolChanger.Services;

namespace KoolChanger.ClientMvvm.Interfaces;

public interface IPartyServiceFactory
{
    PartyService Create(List<Champion> champions, Dictionary<Champion, Skin> selectedSkins, string partyServerUrl);
}
