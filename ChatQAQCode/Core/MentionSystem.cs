using System;
using System.Text.RegularExpressions;
using Godot;
using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.UI;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ChatQAQ.ChatQAQCode.Core;

public class MentionSystem
{
    private static readonly Lazy<MentionSystem> _instance = new(() => new MentionSystem());
    public static MentionSystem Instance => _instance.Value;

    public event Action<string, string>? OnPlayerMentioned;

    public Dictionary<string, PlayerInfo> OnlinePlayers { get; private set; } = new();

    private static readonly Regex MentionPattern = new Regex(
        @"@(?:(?<name>[^\s\[\]]+)|\[(?<name>[^\]]+)\])",
        RegexOptions.Compiled);

    private MentionSystem()
    {
    }

    public List<string> DetectMentions(string content)
    {
        var mentionedPlayers = new List<string>();

        if (string.IsNullOrEmpty(content))
        {
            return mentionedPlayers;
        }

        var matches = MentionPattern.Matches(content);
        foreach (Match match in matches)
        {
            var playerName = match.Groups["name"].Value;
            if (string.IsNullOrEmpty(playerName))
            {
                continue;
            }

            MainFile.Logger.Debug($"DetectMentions: Found mention '{playerName}'");

            var matchingPlayer = OnlinePlayers.Values.FirstOrDefault(p =>
                p.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase) ||
                p.PlayerId.Equals(playerName));

            if (matchingPlayer != null && !mentionedPlayers.Contains(matchingPlayer.PlayerId))
            {
                MainFile.Logger.Debug($"DetectMentions: Matched player {matchingPlayer.PlayerName} ({matchingPlayer.PlayerId})");
                mentionedPlayers.Add(matchingPlayer.PlayerId);
            }
            else
            {
                MainFile.Logger.Debug($"DetectMentions: No match found for '{playerName}'");
            }
        }

        return mentionedPlayers;
    }

    public List<PlayerInfo> GetMatchingPlayers(string partialName)
    {
        if (string.IsNullOrEmpty(partialName))
        {
            return OnlinePlayers.Values.ToList();
        }

        return OnlinePlayers.Values
            .Where(p => p.PlayerName.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    public List<PlayerInfo> GetSortedMatchingPlayers(string partialName, int maxResults = 5)
    {
        var allPlayers = OnlinePlayers.Values.ToList();

        if (string.IsNullOrEmpty(partialName))
        {
            return allPlayers.Take(maxResults).ToList();
        }

        var filtered = allPlayers
            .Where(p => p.PlayerName.IndexOf(partialName, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        filtered.Sort((a, b) =>
        {
            bool aStartsWith = a.PlayerName.StartsWith(partialName, StringComparison.OrdinalIgnoreCase);
            bool bStartsWith = b.PlayerName.StartsWith(partialName, StringComparison.OrdinalIgnoreCase);

            if (aStartsWith && !bStartsWith) return -1;
            if (!aStartsWith && bStartsWith) return 1;

            return string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
        });

        return filtered.Take(maxResults).ToList();
    }

    public void NotifyPlayer(string playerId, string mentionerName, string? messageContent = null)
    {
        if (!OnlinePlayers.TryGetValue(playerId, out var playerInfo) || playerInfo == null)
        {
            MainFile.Logger.Warn($"NotifyPlayer: Player {playerId} not found in OnlinePlayers or playerInfo is null");
            return;
        }

        MainFile.Logger.Info($"NotifyPlayer: Notifying {playerInfo.PlayerName} mentioned by {mentionerName}");

        var config = ConfigManager.Instance.CurrentConfig;

        if (config.EnableMentionNotification)
        {
            ShowNotification(mentionerName, messageContent);
        }

        OnPlayerMentioned?.Invoke(mentionerName, playerId);
    }

    public void UpdateOnlinePlayers(List<PlayerInfo> players)
    {
        OnlinePlayers.Clear();

        if (players == null)
        {
            return;
        }

        foreach (var player in players)
        {
            if (!string.IsNullOrEmpty(player.PlayerId))
            {
                OnlinePlayers[player.PlayerId] = player;
            }
        }
    }

    public string HighlightMention(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
        {
            return playerName;
        }

        if (playerName.Contains(" "))
        {
            return $"[color=yellow]@[{playerName}][/color]";
        }

        return $"[color=yellow]@{playerName}[/color]";
    }

    private void ShowNotification(string mentionerName, string? messageContent)
    {
        try
        {
            MainFile.Logger.Info($"ShowNotification: Creating banner for mention by {mentionerName}");

            var mainText = LocalizationManager.Instance.GetUI("CHATQAQ-MENTION_NOTIFICATION_TITLE");
            var subText = string.IsNullOrEmpty(messageContent)
                ? string.Format(LocalizationManager.Instance.GetUI("CHATQAQ-MENTION_NOTIFICATION"), mentionerName)
                : string.Format(LocalizationManager.Instance.GetUI("CHATQAQ-MENTION_NOTIFICATION_WITH_MESSAGE"), mentionerName, messageContent);

            MainFile.Logger.Info($"ShowNotification: mainText={mainText}, subText={subText}");

            var banner = MentionNotificationBanner.Create(mainText, subText);
            if (banner == null)
            {
                MainFile.Logger.Warn("ShowNotification: Banner.Create returned null (TestMode is on)");
                return;
            }

            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree == null)
            {
                MainFile.Logger.Warn("ShowNotification: SceneTree is null");
                return;
            }

            if (sceneTree.Root == null)
            {
                MainFile.Logger.Warn("ShowNotification: Root is null");
                return;
            }

            MainFile.Logger.Info($"ShowNotification: Adding banner to Root");
            
            var run = sceneTree.Root.GetNodeOrNull("Game/RootSceneContainer/Run");
            if (run != null)
            {
                run.AddChild(banner);
                MainFile.Logger.Info($"ShowNotification: Banner added to Run node");
            }
            else
            {
                sceneTree.Root.AddChild(banner);
                MainFile.Logger.Info($"ShowNotification: Banner added to Root directly");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"ShowNotification failed: {ex}");
        }
    }

    public void AddPlayer(PlayerInfo player)
    {
        if (player != null && !string.IsNullOrEmpty(player.PlayerId))
        {
            OnlinePlayers[player.PlayerId] = player;
            MainFile.Logger.Debug($"Added player to mention system: {player.PlayerName}");
        }
    }

    public void RemovePlayer(string playerId)
    {
        if (!string.IsNullOrEmpty(playerId) && OnlinePlayers.Remove(playerId))
        {
            MainFile.Logger.Debug($"Removed player from mention system: {playerId}");
        }
    }

    public void ClearPlayers()
    {
        OnlinePlayers.Clear();
        MainFile.Logger.Debug("Cleared all players from mention system");
    }
}
