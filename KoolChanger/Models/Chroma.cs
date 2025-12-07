namespace KoolChanger.Models;

public class Chroma : Skin
{
    public List<string> Colors { get; set; } = new();
    public override string ToString()
    {
        return $"Chroma id:{Id} name:{Name} colors: {Colors[0]}";
    }
}
