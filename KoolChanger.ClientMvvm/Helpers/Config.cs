#region

using KoolChanger.Models;

#endregion

namespace KoolChanger.ClientMvvm.Helpers;

public class Config
{
    public Dictionary<string, Skin> SelectedSkins { get; set; } = new();
    public string GamePath { get; set; } = "";
    public string PartyModeUrl { get; set; } = "";
}