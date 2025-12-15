namespace KoolChanger.Client.ViewModels;

public class ChampionListItem
{
    public string IconUrl { get; set; }
    public string Name { get; set; }
    
    public ChampionListItem(string u, string n)
    {
        IconUrl = u;
        Name = n;
    }
}