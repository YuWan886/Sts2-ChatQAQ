namespace ChatQAQ.ChatQAQCode.Data;

public class PlayerInfo
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string CharacterId { get; set; } = "";
    public bool IsLocalPlayer { get; set; }

    public PlayerInfo() { }

    public PlayerInfo(string playerId, string playerName, string characterId, bool isLocalPlayer = false)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        CharacterId = characterId;
        IsLocalPlayer = isLocalPlayer;
    }
}
