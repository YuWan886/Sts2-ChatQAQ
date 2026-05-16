using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.UI;
using ChatQAQ.ChatQAQCode.Networking;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace ChatQAQ.ChatQAQCode.Core;

public class ChatManager : IDisposable
{
    private static readonly Lazy<ChatManager> _instance = new(() => new ChatManager());
    public static ChatManager Instance => _instance.Value;

    public event Action<ChatMessage>? OnMessageSent;
    public event Action<ChatMessage>? OnMessageReceived;
    public event Action? OnInputBoxToggled;

    public bool IsInputBoxVisible { get; private set; }
    public Queue<ChatMessage> MessageQueue { get; private set; }
    public ChatSession? CurrentSession { get; private set; }
    public PlayerInfo? LocalPlayer { get; private set; }

    private readonly ChatHistoryManager _historyManager;
    private readonly MentionSystem _mentionSystem;
    private readonly ConfigManager _configManager;
    private readonly ChatNetworkManager _networkManager;
    private bool _disposed = false;

    private ChatManager()
    {
        MessageQueue = new Queue<ChatMessage>();
        _historyManager = ChatHistoryManager.Instance;
        _mentionSystem = MentionSystem.Instance;
        _configManager = ConfigManager.Instance;
        _networkManager = ChatNetworkManager.Instance;
        _networkManager.OnMessageReceived += OnNetworkMessageReceived;
    }

    public void SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (LocalPlayer == null)
        {
            MainFile.Logger.Warn("Cannot send message: LocalPlayer is not set");
            return;
        }

        var message = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = LocalPlayer.PlayerId,
            SenderName = LocalPlayer.PlayerName,
            Content = content,
            Timestamp = DateTime.Now,
            PlayTime = GetPlayTime(),
            SessionId = CurrentSession?.SessionId ?? string.Empty,
            IsLocalPlayer = true,
            CharacterId = LocalPlayer.CharacterId ?? string.Empty
        };

        message.MentionedPlayerIds = _mentionSystem.DetectMentions(content);

        _historyManager.AddMessage(message);
        _historyManager.SaveToFile();

        ShowSpeechBubble(content, LocalPlayer.PlayerId);

        _networkManager.BroadcastMessage(message);

        OnMessageSent?.Invoke(message);
    }

    private void OnNetworkMessageReceived(ChatMessage message)
    {
        if (message == null)
        {
            MainFile.Logger.Warn("OnNetworkMessageReceived: message is null");
            return;
        }

        MainFile.Logger.Info($"ChatManager: Received network message from {message.SenderName}");

        message.MentionedPlayerIds = _mentionSystem.DetectMentions(message.Content);
        message.SessionId = CurrentSession?.SessionId ?? string.Empty;
        message.PlayTime = GetPlayTime();
        MainFile.Logger.Info($"ChatManager: Detected {message.MentionedPlayerIds.Count} mentions in message");

        _historyManager.AddMessage(message);
        _historyManager.SaveToFile();

        MessageQueue.Enqueue(message);

        ShowSpeechBubble(message.Content, message.SenderId);

        ProcessMessageQueue();

        OnMessageReceived?.Invoke(message);
    }

    public void ToggleInputBox()
    {
        IsInputBoxVisible = !IsInputBoxVisible;
        OnInputBoxToggled?.Invoke();
    }

    public void ShowInputBox()
    {
        if (!IsInputBoxVisible)
        {
            IsInputBoxVisible = true;
            OnInputBoxToggled?.Invoke();
        }
    }

    public void HideInputBox()
    {
        if (IsInputBoxVisible)
        {
            IsInputBoxVisible = false;
            OnInputBoxToggled?.Invoke();
        }
    }

    public void StartNewSession()
    {
        if (CurrentSession != null && !CurrentSession.IsEnded)
        {
            EndCurrentSession();
        }

        CurrentSession = new ChatSession
        {
            SessionId = Guid.NewGuid().ToString(),
            StartTime = DateTime.Now,
            CharacterId = GetCurrentCharacterId()
        };

        _historyManager.AddSession(CurrentSession);
        _historyManager.SaveToFile();

        MainFile.Logger.Info($"New chat session started: {CurrentSession.SessionId}");
    }

    public void EndCurrentSession()
    {
        if (CurrentSession == null)
        {
            return;
        }

        CurrentSession.EndTime = DateTime.Now;
        _historyManager.UpdateSession(CurrentSession);
        _historyManager.SaveToFile();

        MainFile.Logger.Info($"Chat session ended: {CurrentSession.SessionId}");

        CurrentSession = null;
    }

    public void SetLocalPlayer(PlayerInfo player)
    {
        LocalPlayer = player;
        MainFile.Logger.Info($"Local player set: {player.PlayerName} ({player.PlayerId})");
    }

    public void ProcessMessageQueue()
    {
        if (LocalPlayer == null)
        {
            MainFile.Logger.Warn("ProcessMessageQueue: LocalPlayer is null");
            return;
        }

        while (MessageQueue.Count > 0)
        {
            var message = MessageQueue.Dequeue();

            foreach (var playerId in message.MentionedPlayerIds)
            {
                if (playerId == LocalPlayer.PlayerId)
                {
                    _mentionSystem.NotifyPlayer(playerId, message.SenderName, message.Content);
                }
            }

            MainFile.Logger.Debug($"Processed message bubble: {message.Content}");
        }
    }

    private void ShowSpeechBubble(string content, string senderId)
    {
        if (RunManager.Instance == null)
        {
            MainFile.Logger.Warn("ShowSpeechBubble: RunManager.Instance is null");
            return;
        }

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null)
        {
            MainFile.Logger.Warn("ShowSpeechBubble: runState is null");
            return;
        }

        if (runState.Players == null)
        {
            MainFile.Logger.Warn("ShowSpeechBubble: runState.Players is null");
            return;
        }

        var customTags = BBcodeTagHelper.Instance.ExtractCustomTags(content);
        var hasCustomTags = customTags.Count > 0;

        var sanitizedContent = BBcodeTagHelper.Instance.ValidateAndFixBbcode(content);
        sanitizedContent = BBcodeTagHelper.Instance.EscapeMentionNames(sanitizedContent);
        var displayContent = BBcodeTagHelper.Instance.ConvertCustomTagsToBbcode(sanitizedContent);

        foreach (var player in runState.Players)
        {
            var playerIdStr = player.NetId.ToString();
            if (playerIdStr == senderId)
            {
                var creature = player.Creature;
                if (creature == null || creature.IsDead)
                {
                    MainFile.Logger.Warn("Cannot show speech bubble: Creature is null or dead");
                    continue;
                }

                var duration = _configManager.CurrentConfig?.BubbleDisplayDuration ?? 3.0f;
                MainFile.Logger.Info($"ChatManager.ShowSpeechBubble: BubbleDisplayDuration from config = {_configManager.CurrentConfig?.BubbleDisplayDuration}, duration = {duration}");

                try
                {
                    var combatRoom = NCombatRoom.Instance;
                    Node? parentNode;
                    bool isInCombat;

                    if (combatRoom != null)
                    {
                        parentNode = combatRoom.CombatVfxContainer;
                        isInCombat = true;
                    }
                    else
                    {
                        // Non-combat room: use NRun.Instance as parent
                        parentNode = (Node?)NRun.Instance ?? NGame.Instance;
                        isInCombat = false;
                        if (parentNode == null)
                        {
                            MainFile.Logger.Warn("ShowSpeechBubble: No parent node available (NRun and NGame are null)");
                            return;
                        }
                    }

                    // Calculate bubble position for non-combat rooms
                    Vector2? overridePosition = null;
                    if (!isInCombat)
                    {
                        overridePosition = GetBubblePosition(player, creature, runState);
                    }

                    if (hasCustomTags || !isInCombat)
                    {
                        // Use ChatBubbleWithHoverTips for custom tags OR non-combat rooms
                        // (NSpeechBubbleVfx.Create with Creature overload requires NCombatRoom.Instance)
                        var bubbleContent = content;
                        if (!isInCombat)
                        {
                            // Prepend sender name in non-combat rooms so players can identify who is talking
                            var senderName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
                            bubbleContent = $"{senderName}：{content}";
                        }
                        var bubble = new ChatBubbleWithHoverTips();
                        bubble.Setup(bubbleContent, creature!, duration, overridePosition);
                        parentNode.AddChild(bubble);
                        bubble.AnimateIn();
                    }
                    else
                    {
                        var bubble = NSpeechBubbleVfx.Create(displayContent, creature, duration, VfxColor.White);
                        if (bubble != null)
                        {
                            parentNode.AddChild(bubble);
                        }
                    }

                    MainFile.Logger.Info($"ChatManager: Showed speech bubble for player {playerIdStr}");
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn($"Failed to show speech bubble: {ex.Message}");
                }

                break;
            }
        }
    }

    /// <summary>
    /// Calculate the screen position for a speech bubble in a non-combat room.
    /// Returns null if in combat (let NCreature-based positioning handle it).
    /// </summary>
    private static Vector2? GetBubblePosition(Player player, Creature creature, RunState runState)
    {
        try
        {
            // 1. Try NRestSiteRoom: use NRestSiteCharacter positions
            var restSite = NRestSiteRoom.Instance;
            if (restSite != null)
            {
                foreach (var character in restSite.Characters)
                {
                    if (character.Player.NetId == player.NetId)
                    {
                        // Use the character's thought anchor or global position
                        var pos = character.GlobalPosition;
                        // Offset upward to appear above the character's head
                        pos.Y -= 120f;
                        return pos;
                    }
                }
            }

            // 2. Try NMerchantRoom: use PlayerVisuals list matched by player index
            var merchant = NMerchantRoom.Instance;
            if (merchant != null)
            {
                var visuals = merchant.PlayerVisuals;
                // Build a mapping: find the index of this player among same-side players
                int merchantIndex = 0;
                foreach (var p in runState.Players)
                {
                    if (p.Creature?.Side == creature.Side)
                    {
                        if (p.NetId == player.NetId && merchantIndex < visuals.Count)
                        {
                            var pos = visuals[merchantIndex].GlobalPosition;
                            pos.Y -= 120f;
                            return pos;
                        }
                        merchantIndex++;
                    }
                }
                // If not matched, fall through to calculated position
            }

            // 3. For rooms without character visuals, calculate position based on player side and index
            return CalculateDefaultBubblePosition(player, creature, runState);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"GetBubblePosition failed: {ex.Message}");
            return CalculateDefaultBubblePosition(player, creature, runState);
        }
    }

    /// <summary>
    /// Calculate a default bubble position based on the player's side and their index
    /// among same-side players. Player-side characters are on the left, enemies on the right.
    /// </summary>
    private static Vector2 CalculateDefaultBubblePosition(Player player, Creature creature, RunState runState)
    {
        CombatSide side = creature.Side;
        
        // Count same-side players to determine offsets
        int sameSideIndex = 0;
        int sameSideCount = 0;
        foreach (var p in runState.Players)
        {
            if (p.Creature?.Side == side)
            {
                if (p.NetId == player.NetId)
                {
                    sameSideIndex = sameSideCount;
                }
                sameSideCount++;
            }
        }

        float xPos;
        if (side == CombatSide.Player)
        {
            // Player-side: position on the left side of screen
            // Spread players vertically and slightly horizontally
            float baseX = 200f + sameSideIndex * 60f;
            xPos = baseX;
        }
        else
        {
            // Enemy-side: position on the right side of screen
            float baseX = 1720f - sameSideIndex * 60f;
            xPos = baseX;
        }

        // Vertical position: center area, offset by index
        float yPos = 400f - sameSideIndex * 100f;

        return new Vector2(xPos, yPos);
    }

    /// <summary>
    /// Recursively find the first node of type T in the tree.
    /// </summary>
    private static T? FindNodeOfType<T>(Node root) where T : Node
    {
        if (root is T match) return match;
        foreach (var child in root.GetChildren())
        {
            var result = FindNodeOfType<T>(child);
            if (result != null) return result;
        }
        return default;
    }

    private TimeSpan GetPlayTime()
    {
        return TimeSpan.Zero;
    }

    private string GetCurrentCharacterId()
    {
        return LocalPlayer?.CharacterId ?? string.Empty;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _networkManager.OnMessageReceived -= OnNetworkMessageReceived;

        _disposed = true;
    }
}
