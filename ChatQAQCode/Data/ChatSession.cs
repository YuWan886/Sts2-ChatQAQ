namespace ChatQAQ.ChatQAQCode.Data;

public class ChatSession
{
    public string SessionId { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string CharacterId { get; set; } = null!;
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public bool IsEnded => EndTime.HasValue;
}
