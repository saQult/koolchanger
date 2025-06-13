namespace CSLOLTool.Models;

public class LobbyMember
{
    public string Puuid { get; set; } = string.Empty;
    public bool IsLeader { get; set; }
}

public class LocalMember
{
    public string Puuid { get; set; } = string.Empty;
}

public class LobbyData
{
    public LocalMember LocalMember { get; set; } = default!;
    public List<LobbyMember> Members { get; set; } = [];
    public string LobbyId { get; set; } = string.Empty;
}