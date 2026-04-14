using System;
using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.Core;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace ChatQAQ.ChatQAQCode.Networking;

public class ChatNetworkManager : IDisposable
{
    private static readonly Lazy<ChatNetworkManager> _instance = new(() => new ChatNetworkManager());
    public static ChatNetworkManager Instance => _instance.Value;

    public event Action<ChatMessage>? OnMessageReceived;

    public bool IsConnected { get; private set; }
    public bool IsMultiplayer { get; private set; }
    private bool _isInitialized = false;
    private bool _disposed = false;

    private ChatNetworkManager()
    {
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            MainFile.Logger.Info("ChatNetworkManager: Already initialized, updating multiplayer status");
            CheckMultiplayerStatus();
            UpdateOnlinePlayersList();
            return;
        }

        MainFile.Logger.Info("ChatNetworkManager: Initializing...");
        CheckMultiplayerStatus();
        RegisterMessageHandlers();
        UpdateOnlinePlayersList();
        _isInitialized = true;
        MainFile.Logger.Info("ChatNetworkManager: Initialization complete");
    }

    public void UpdateOnlinePlayersList()
    {
        var players = GetOnlinePlayers();
        MainFile.Logger.Info($"ChatNetworkManager: Updating online players list, found {players.Count} players");
        MentionSystem.Instance.UpdateOnlinePlayers(players);
    }

    private void RegisterMessageHandlers()
    {
        var netService = RunManager.Instance?.NetService;
        if (netService != null)
        {
            netService.RegisterMessageHandler<ChatBubbleMessage>(HandleChatBubbleMessage);
            MainFile.Logger.Info("ChatNetworkManager: Registered ChatBubbleMessage handler");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        var netService = RunManager.Instance?.NetService;
        if (netService != null)
        {
            netService.UnregisterMessageHandler<ChatBubbleMessage>(HandleChatBubbleMessage);
        }

        _disposed = true;
    }

    public void BroadcastMessage(ChatMessage message)
    {
        if (!IsMultiplayer) return;

        var netService = RunManager.Instance?.NetService;
        if (netService == null || !netService.IsConnected)
        {
            MainFile.Logger.Warn("ChatNetworkManager: Cannot broadcast - NetService not available");
            return;
        }

        var bubbleMessage = new ChatBubbleMessage
        {
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            Content = message.Content,
            Duration = 3.0f,
            CharacterId = message.CharacterId ?? string.Empty
        };

        try
        {
            netService.SendMessage(bubbleMessage);
            MainFile.Logger.Info($"ChatNetworkManager: Broadcasted message from {message.SenderName}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"ChatNetworkManager: Failed to broadcast message: {ex.Message}");
        }
    }

    public void SendToPlayer(string playerId, ChatMessage message)
    {
        if (!IsMultiplayer) return;

        BroadcastMessage(message);
    }

    private void HandleChatBubbleMessage(ChatBubbleMessage message, ulong senderId)
    {
        MainFile.Logger.Info($"ChatNetworkManager: Received chat bubble from {message.SenderName} (ID: {senderId})");

        var chatMessage = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            Content = message.Content,
            Timestamp = DateTime.Now,
            PlayTime = TimeSpan.Zero,
            SessionId = string.Empty,
            IsLocalPlayer = false,
            CharacterId = message.CharacterId ?? string.Empty
        };

        OnMessageReceived?.Invoke(chatMessage);
    }

    public void CheckMultiplayerStatus()
    {
        var netService = RunManager.Instance?.NetService;
        if (netService != null)
        {
            IsMultiplayer = netService.Type.IsMultiplayer();
            IsConnected = netService.IsConnected;
            MainFile.Logger.Info($"ChatNetworkManager: Multiplayer status - IsMultiplayer: {IsMultiplayer}, IsConnected: {IsConnected}");
        }
        else
        {
            IsMultiplayer = false;
            IsConnected = false;
        }
    }

    private bool IsInMultiplayerGame()
    {
        var netService = RunManager.Instance?.NetService;
        return netService != null && netService.Type.IsMultiplayer();
    }

    private string SerializeMessage(ChatMessage message)
    {
        return System.Text.Json.JsonSerializer.Serialize(message);
    }

    private ChatMessage? DeserializeMessage(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ChatMessage>(json);
        }
        catch
        {
            return null;
        }
    }

    private void SendToAllPlayers(string data)
    {
    }

    private void SendToSpecificPlayer(string playerId, string data)
    {
    }

    public void OnPlayerJoined(string playerId, string playerName)
    {
        MentionSystem.Instance.UpdateOnlinePlayers(GetOnlinePlayers());
    }

    public void OnPlayerLeft(string playerId)
    {
        MentionSystem.Instance.UpdateOnlinePlayers(GetOnlinePlayers());
    }

    private List<PlayerInfo> GetOnlinePlayers()
    {
        var players = new List<PlayerInfo>();
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState != null)
        {
            foreach (var player in runState.Players)
            {
                players.Add(new PlayerInfo
                {
                    PlayerId = player.NetId.ToString(),
                    PlayerName = player.Creature?.Name ?? "Unknown",
                    CharacterId = player.Character?.Id.Entry ?? ""
                });
            }
        }
        return players;
    }
}
