#region

using KoolChanger.Core.Models;

#endregion

namespace KoolChanger.Client.Helpers;

public class Config
{
    public Dictionary<string, Skin> SelectedSkins { get; set; } = new();
    public string GamePath { get; set; } = "";
    public string PartyModeUrl { get; set; } = "";
}