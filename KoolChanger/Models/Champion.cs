using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace KoolChanger.Models;

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

    public override string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public override int GetHashCode() => Id.GetHashCode();
}


