namespace KoolChanger.Models;

public class CustomSkin
{
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not CustomSkin customSkin)
            return false;

        return (customSkin.Author, customSkin.Description, customSkin.Name, customSkin.Version) == (Author, Description, Name, Version);
    }
    public override int GetHashCode()
    {
        var skin = Version + Name + Author;
        return skin.GetHashCode();
    }
}
