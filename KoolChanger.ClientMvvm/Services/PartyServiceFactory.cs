#region

using System.Collections.Generic;
using KoolChanger.ClientMvvm.Interfaces;
using KoolChanger.Models;
using KoolChanger.Services;

#endregion

namespace KoolChanger.ClientMvvm.Services;

public class PartyServiceFactory : IPartyServiceFactory
{
    public PartyService Create(List<Champion> champions, Dictionary<Champion, Skin> selectedSkins, string partyServerUrl)
    {
        return new PartyService(champions, selectedSkins, partyServerUrl);
    }
}
