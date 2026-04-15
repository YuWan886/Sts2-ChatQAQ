using Godot;
using ChatQAQ.ChatQAQCode.Data;

namespace ChatQAQ.ChatQAQCode.Core;

public class HotkeyManager
{
    private static readonly Lazy<HotkeyManager> _instance = new(() => new HotkeyManager());
    public static HotkeyManager Instance => _instance.Value;

    public event Action? OnChatHotkeyPressed;
    public event Action<Vector2>? OnQuickSendTriggered;
    public event Action<bool>? OnModifierKeyStateChanged;

    public Key ChatHotkey { get; private set; } = Key.T;
    public bool IsEnabled { get; set; } = true;
    public bool IsInputFocused { get; set; } = false;
    public bool IsModifierKeyPressed { get; private set; } = false;

    private bool _isModifierPressed = false;
    private Key _quickSendModifierKey = Key.Ctrl;
    private MouseButton _quickSendMouseButton = MouseButton.Right;
    private bool _quickSendEnabled = true;

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

        ProcessModifierKey(@event);

        if (IsInputFocused) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == ChatHotkey)
            {
                OnChatHotkeyPressed?.Invoke();
            }
        }

        ProcessQuickSendInput(@event);
    }

    private void ProcessModifierKey(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            bool isModifier = IsModifierKey(keyEvent.Keycode);

            if (isModifier)
            {
                if (keyEvent.Pressed && !_isModifierPressed)
                {
                    _isModifierPressed = true;
                    IsModifierKeyPressed = true;
                    OnModifierKeyStateChanged?.Invoke(true);
                }
                else if (!keyEvent.Pressed && _isModifierPressed)
                {
                    _isModifierPressed = false;
                    IsModifierKeyPressed = false;
                    OnModifierKeyStateChanged?.Invoke(false);
                }
            }
        }
    }

    private bool IsModifierKey(Key keycode)
    {
        if (keycode == _quickSendModifierKey) return true;

        if (_quickSendModifierKey == Key.Ctrl)
        {
            return keycode == Key.Ctrl;
        }
        if (_quickSendModifierKey == Key.Shift)
        {
            return keycode == Key.Shift;
        }
        if (_quickSendModifierKey == Key.Alt)
        {
            return keycode == Key.Alt;
        }

        return false;
    }

    private void ProcessQuickSendInput(InputEvent @event)
    {
        if (!_quickSendEnabled) return;
        if (IsInputFocused) return;
        if (!_isModifierPressed) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == _quickSendMouseButton)
            {
                OnQuickSendTriggered?.Invoke(mouseEvent.Position);
                MainFile.Logger.Info($"QuickSend triggered: Ctrl+Right Click at {mouseEvent.Position}");
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

    public void SetQuickSendModifierKey(Key key)
    {
        _quickSendModifierKey = key;
        if (ConfigManager.Instance.CurrentConfig != null)
        {
            ConfigManager.Instance.CurrentConfig.QuickSendModifierKey = key;
            ConfigManager.Instance.Save();
        }
    }

    public void SetQuickSendMouseButton(MouseButton button)
    {
        _quickSendMouseButton = button;
        if (ConfigManager.Instance.CurrentConfig != null)
        {
            ConfigManager.Instance.CurrentConfig.QuickSendMouseButton = button;
            ConfigManager.Instance.Save();
        }
    }

    public void SetQuickSendEnabled(bool enabled)
    {
        _quickSendEnabled = enabled;
        if (ConfigManager.Instance.CurrentConfig != null)
        {
            ConfigManager.Instance.CurrentConfig.QuickSendEnabled = enabled;
            ConfigManager.Instance.Save();
        }
    }

    public Key GetQuickSendModifierKey()
    {
        return _quickSendModifierKey;
    }

    public MouseButton GetQuickSendMouseButton()
    {
        return _quickSendMouseButton;
    }

    public bool IsQuickSendEnabled()
    {
        return _quickSendEnabled;
    }

    private void LoadHotkeyFromConfig()
    {
        var config = ConfigManager.Instance.CurrentConfig;
        if (config != null)
        {
            ChatHotkey = config.Hotkey;
            _quickSendEnabled = config.QuickSendEnabled;
            _quickSendModifierKey = config.QuickSendModifierKey;
            _quickSendMouseButton = config.QuickSendMouseButton;
        }
    }
}
