namespace ChatQAQ.ChatQAQCode.Data;

public class ChatMessage
{
    public string MessageId { get; set; } = null!;
    public string SenderId { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string CharacterId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public TimeSpan PlayTime { get; set; }
    public string SessionId { get; set; } = null!;
    public bool IsLocalPlayer { get; set; }
    public List<string> MentionedPlayerIds { get; set; } = new List<string>();
}
