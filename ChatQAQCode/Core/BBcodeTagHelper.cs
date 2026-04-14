using System.Text.RegularExpressions;
using Godot;

namespace ChatQAQ.ChatQAQCode.Core;

public partial class BBcodeTagHelper : RefCounted
{
    private static readonly Lazy<BBcodeTagHelper> _instance = new(() => new BBcodeTagHelper());
    public static BBcodeTagHelper Instance => _instance.Value;

    private static readonly Regex CardTagPattern = new Regex(
        @"\[card=([^\]]*)\]([^\[]*)\[/card\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PotionTagPattern = new Regex(
        @"\[potion=([^\]]*)\]([^\[]*)\[/potion\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RelicTagPattern = new Regex(
        @"\[relic=([^\]]*)\]([^\[]*)\[/relic\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Color CardColor { get; set; } = new Color("FFD700");
    public Color PotionColor { get; set; } = new Color("00CED1");
    public Color RelicColor { get; set; } = new Color("DA70D6");

    public string CreateCardTag(string cardId, string displayName)
    {
        return $"[card={cardId}]{displayName}[/card]";
    }

    public string CreatePotionTag(string potionId, string displayName)
    {
        return $"[potion={potionId}]{displayName}[/potion]";
    }

    public string CreateRelicTag(string relicId, string displayName)
    {
        return $"[relic={relicId}]{displayName}[/relic]";
    }

    public string CreateColorTag(string color, string text)
    {
        return $"[color={color}]{text}[/color]";
    }

    public string CreateBoldTag(string text)
    {
        return $"[b]{text}[/b]";
    }

    public string CreateItalicTag(string text)
    {
        return $"[i]{text}[/i]";
    }

    public string CreateUnderlineTag(string text)
    {
        return $"[u]{text}[/u]";
    }

    public string ConvertCustomTagsToBbcode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        result = CardTagPattern.Replace(result, match =>
        {
            var cardId = match.Groups[1].Value;
            var displayName = match.Groups[2].Value;
            return FormatItemTag("card", cardId, displayName, CardColor);
        });

        result = PotionTagPattern.Replace(result, match =>
        {
            var potionId = match.Groups[1].Value;
            var displayName = match.Groups[2].Value;
            return FormatItemTag("potion", potionId, displayName, PotionColor);
        });

        result = RelicTagPattern.Replace(result, match =>
        {
            var relicId = match.Groups[1].Value;
            var displayName = match.Groups[2].Value;
            return FormatItemTag("relic", relicId, displayName, RelicColor);
        });

        return result;
    }

    public string SanitizeBbcode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        result = Regex.Replace(result, @"\[/([^\]]*)\]", match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            if (!tagName.StartsWith("/"))
            {
                return $"[/{tagName}]";
            }
            return match.Value;
        });

        result = Regex.Replace(result, @"\[([^\]=/]+)=([^\]]*)\]", match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            var tagValue = match.Groups[2].Value.Trim();
            return $"[{tagName}={tagValue}]";
        });

        result = Regex.Replace(result, @"\[([^\]=/]+)\]", match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            if (tagName.StartsWith("/"))
            {
                return match.Value;
            }
            return $"[{tagName}]";
        });

        return result;
    }

    public string ValidateAndFixBbcode(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        result = Regex.Replace(result, @"\[(b|i|u)\]([^\[]*?)/\1\]", match =>
        {
            var tagName = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            return $"[{tagName}]{content}[/{tagName}]";
        });

        result = Regex.Replace(result, @"\[color=([^\]]+)\]([^\[]*?)/color\]", match =>
        {
            var color = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            return $"[color={color}]{content}[/color]";
        });

        return result;
    }

    public string EscapeBbcodeBrackets(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Replace("[", "[lb]").Replace("]", "[rb]");
    }

    public string EscapeMentionNames(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        result = Regex.Replace(result, @"@\[([^\]]+)\]", match =>
        {
            var playerName = match.Groups[1].Value;
            return $"@[lb]{playerName}[rb]";
        });

        return result;
    }

    private string FormatItemTag(string type, string id, string displayName, Color color)
    {
        var colorHex = color.ToHtml(false);
        var prefix = type switch
        {
            "card" => "🃏 ",
            "potion" => "🧪 ",
            "relic" => "💎 ",
            _ => ""
        };
        return $"[color=#{colorHex}][u]{prefix}{displayName}[/u][/color]";
    }

    public List<TagMatch> ExtractCustomTags(string input)
    {
        var tags = new List<TagMatch>();

        if (string.IsNullOrEmpty(input))
        {
            return tags;
        }

        foreach (Match match in CardTagPattern.Matches(input))
        {
            tags.Add(new TagMatch
            {
                Type = TagType.Card,
                Id = match.Groups[1].Value,
                DisplayName = match.Groups[2].Value,
                FullMatch = match.Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        foreach (Match match in PotionTagPattern.Matches(input))
        {
            tags.Add(new TagMatch
            {
                Type = TagType.Potion,
                Id = match.Groups[1].Value,
                DisplayName = match.Groups[2].Value,
                FullMatch = match.Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        foreach (Match match in RelicTagPattern.Matches(input))
        {
            tags.Add(new TagMatch
            {
                Type = TagType.Relic,
                Id = match.Groups[1].Value,
                DisplayName = match.Groups[2].Value,
                FullMatch = match.Value,
                Index = match.Index,
                Length = match.Length
            });
        }

        tags.Sort((a, b) => a.Index.CompareTo(b.Index));
        return tags;
    }

    public string GetTagColor(TagType type)
    {
        return type switch
        {
            TagType.Card => CardColor.ToHtml(false),
            TagType.Potion => PotionColor.ToHtml(false),
            TagType.Relic => RelicColor.ToHtml(false),
            _ => "FFFFFF"
        };
    }

    public string GetTagPrefix(TagType type)
    {
        return type switch
        {
            TagType.Card => "🃏",
            TagType.Potion => "🧪",
            TagType.Relic => "💎",
            _ => ""
        };
    }

    public enum TagType
    {
        Card,
        Potion,
        Relic
    }

    public class TagMatch
    {
        public TagType Type { get; set; }
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string FullMatch { get; set; } = "";
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
