#region

using System.Collections.Generic;
using KoolChanger.Client.Interfaces;
using KoolChanger.Core.Models;
using KoolChanger.Core.Services;

#endregion

namespace KoolChanger.Client.Services;

public class PartyServiceFactory : IPartyServiceFactory
{
    public PartyService Create(List<Champion> champions, Dictionary<Champion, Skin> selectedSkins, string partyServerUrl)
    {
        return new PartyService(champions, selectedSkins, partyServerUrl);
    }
}
