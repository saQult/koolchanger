namespace CSLOLTool.Models;

public class Champion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Skin> Skins { get; set; } = [];

    public override bool Equals(object? obj)
    {
        if (obj is not Champion || obj is null)
            return false;
        return (obj as Champion)!.Id == Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
