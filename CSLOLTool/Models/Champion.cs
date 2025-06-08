namespace CSLOLTool.Models;

public class Champion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Skin> Skins { get; set; } = new();

}
