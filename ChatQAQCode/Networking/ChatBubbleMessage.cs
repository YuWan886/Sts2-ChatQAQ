using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;

namespace ChatQAQ.ChatQAQCode.Networking;

public struct ChatBubbleMessage : INetMessage, IPacketSerializable
{
    public required string SenderId;
    public required string SenderName;
    public required string Content;
    public required float Duration;
    public string CharacterId;

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Info;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteString(SenderId);
        writer.WriteString(SenderName);
        writer.WriteString(Content);
        writer.WriteFloat(Duration);
        writer.WriteString(CharacterId ?? string.Empty);
    }

    public void Deserialize(PacketReader reader)
    {
        SenderId = reader.ReadString();
        SenderName = reader.ReadString();
        Content = reader.ReadString();
        Duration = reader.ReadFloat();
        CharacterId = reader.ReadString();
    }
}
