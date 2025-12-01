namespace KoolChanger.Models;

public class Skin
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public List<Chroma> Chromas { get; set; } = new();
}
