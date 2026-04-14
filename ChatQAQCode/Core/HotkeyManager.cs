using System;
using Godot;
using ChatQAQ.ChatQAQCode.Data;

namespace ChatQAQ.ChatQAQCode.Core;

public class HotkeyManager
{
    private static readonly Lazy<HotkeyManager> _instance = new(() => new HotkeyManager());
    public static HotkeyManager Instance => _instance.Value;

    public event Action? OnChatHotkeyPressed;

    public Key ChatHotkey { get; private set; } = Key.T;
    public bool IsEnabled { get; set; } = true;
    public bool IsInputFocused { get; set; } = false;

    private HotkeyManager()
    {
        LoadHotkeyFromConfig();
    }

    public void Initialize()
    {
        LoadHotkeyFromConfig();
    }

    public void SetHotkey(Key key)
    {
        ChatHotkey = key;
        if (ConfigManager.Instance.CurrentConfig != null)
        {
            ConfigManager.Instance.CurrentConfig.Hotkey = key;
            ConfigManager.Instance.Save();
        }
    }

    public void ProcessInput(InputEvent @event)
    {
        if (!IsEnabled) return;
        if (IsInputFocused) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == ChatHotkey)
            {
                OnChatHotkeyPressed?.Invoke();
            }
        }
    }

    public bool IsHotkeyPressed(InputEvent @event)
    {
        if (!IsEnabled) return false;
        if (IsInputFocused) return false;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            return keyEvent.Keycode == ChatHotkey;
        }
        return false;
    }

    public string GetHotkeyName()
    {
        return ChatHotkey.ToString();
    }

    private void LoadHotkeyFromConfig()
    {
        var config = ConfigManager.Instance.CurrentConfig;
        if (config != null)
        {
            ChatHotkey = config.Hotkey;
        }
    }
}
