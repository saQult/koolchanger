namespace KoolChanger.Backend.Models;

public class Lobby
{
    public string LobbyId { get; set; } = "";
    public List<LobbyMember> Members { get; set; } = new();
}