using Godot;

namespace ChatQAQ.ChatQAQCode.Data;

[AttributeUsage(AttributeTargets.Property)]
public class SavedPropertyAttribute : Attribute
{
}

public class ChatConfig
{
    [SavedProperty]
    public Key Hotkey { get; set; } = Key.T;

    [SavedProperty]
    public float BubbleDisplayDuration { get; set; } = 3.0f;

    [SavedProperty]
    public float InputBoxPositionX { get; set; } = -1.0f;

    [SavedProperty]
    public float InputBoxPositionY { get; set; } = -1.0f;

    [SavedProperty]
    public int MaxHistoryMessages { get; set; } = 1000;

    [SavedProperty]
    public bool EnableMentionSound { get; set; } = true;

    [SavedProperty]
    public bool EnableMentionNotification { get; set; } = true;

    [SavedProperty]
    public float MentionSoundVolume { get; set; } = 1.0f;

    [SavedProperty]
    public float MinNotificationDuration { get; set; } = 3.0f;

    [SavedProperty]
    public float MaxNotificationDuration { get; set; } = 7.0f;

    [SavedProperty]
    public int MaxNotificationTextLength { get; set; } = 60;

    [SavedProperty]
    public bool EnableMentionAutocomplete { get; set; } = true;

    [SavedProperty]
    public int MaxSuggestionResults { get; set; } = 5;

    [SavedProperty]
    public float AutocompleteDebounceMs { get; set; } = 300f;

    [SavedProperty]
    public bool QuickSendEnabled { get; set; } = true;

    [SavedProperty]
    public Key QuickSendModifierKey { get; set; } = Key.Ctrl;

    [SavedProperty]
    public MouseButton QuickSendMouseButton { get; set; } = MouseButton.Right;

    public static ChatConfig CreateDefault()
    {
        return new ChatConfig();
    }
}
