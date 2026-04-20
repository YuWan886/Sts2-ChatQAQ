using Godot;
using ChatQAQ.ChatQAQCode.Core;
using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.Networking;
using System.Collections.Generic;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class ChatInputBox : Control
{
    [Signal]
    public delegate void SendMessageEventHandler(string content);

    [Signal]
    public delegate void OpenSettingsEventHandler();

    [Signal]
    public delegate void OpenHistoryEventHandler();

    [Signal]
    public delegate void CloseRequestedEventHandler();

    private Panel _backgroundPanel = null!;
    private VBoxContainer _mainVBox = null!;
    private HBoxContainer _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeBtn = null!;
    private ChatToolbar _toolbar = null!;
    private HBoxContainer _contentRow = null!;
    private LineEdit _lineEdit = null!;
    private Button _settingsBtn = null!;
    private Button _historyBtn = null!;
    private Button _sendBtn = null!;
    private MentionSuggestionPanel _suggestionPanel = null!;

    private bool _isDragging = false;
    private Vector2 _dragStartPos;
    private Tween _tween = null!;
    private bool _isMouseInside = false;
    private bool _isMentionMode = false;
    private int _mentionStartIndex = -1;
    private double _lastInputTime;
    private const double _debounceDelay = 0.3;

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        CustomMinimumSize = new Vector2(420, 130);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkEnd;

        HotkeyManager.Instance.OnChatHotkeyPressed += Toggle;

        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundPanel.CustomMinimumSize = new Vector2(420, 130);
        AddChild(_backgroundPanel);

        var styleBox = StsUiStyles.CreatePanelStyle(borderRadius: 6, borderWidth: 2, padding: 0);
        _backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);

        _mainVBox = new VBoxContainer();
        _mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainVBox.AddThemeConstantOverride("separation", 0);
        _backgroundPanel.AddChild(_mainVBox);

        _titleBar = new HBoxContainer();
        _titleBar.CustomMinimumSize = new Vector2(0, 28);
        _titleBar.AddThemeConstantOverride("separation", 8);
        var titleBarStyle = new StyleBoxFlat();
        titleBarStyle.BgColor = new Color(0.08f, 0.06f, 0.04f, 1.0f);
        titleBarStyle.BorderColor = StsUiStyles.PanelBorder;
        titleBarStyle.SetBorderWidthAll(0);
        titleBarStyle.SetContentMarginAll(6);
        _titleBar.AddThemeStyleboxOverride("panel", titleBarStyle);
        _mainVBox.AddChild(_titleBar);

        var dragHint = new Label();
        dragHint.Text = "⋮⋮";
        dragHint.AddThemeColorOverride("font_color", StsUiStyles.TextMuted);
        dragHint.AddThemeFontSizeOverride("font_size", 12);
        dragHint.MouseFilter = MouseFilterEnum.Pass;
        _titleBar.AddChild(dragHint);

        _titleLabel = new Label();
        _titleLabel.Text = LocalizationManager.Instance.GetUI("CHATQAQ-CHAT");
        _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.VerticalAlignment = VerticalAlignment.Center;
        _titleLabel.AddThemeColorOverride("font_color", StsUiStyles.Gold);
        _titleLabel.AddThemeFontSizeOverride("font_size", 14);
        _titleLabel.MouseFilter = MouseFilterEnum.Pass;
        _titleBar.AddChild(_titleLabel);

        _closeBtn = StsUiStyles.CreateCloseButton();
        _closeBtn.Pressed += OnClosePressed;
        _titleBar.AddChild(_closeBtn);

        var separator1 = new HSeparator();
        separator1.CustomMinimumSize = new Vector2(0, 2);
        var sepStyle1 = new StyleBoxFlat();
        sepStyle1.BgColor = StsUiStyles.PanelBorder;
        separator1.AddThemeStyleboxOverride("separator", sepStyle1);
        _mainVBox.AddChild(separator1);

        var toolbarContainer = new MarginContainer();
        toolbarContainer.AddThemeConstantOverride("margin_left", 6);
        toolbarContainer.AddThemeConstantOverride("margin_right", 6);
        toolbarContainer.AddThemeConstantOverride("margin_top", 4);
        toolbarContainer.AddThemeConstantOverride("margin_bottom", 4);
        _mainVBox.AddChild(toolbarContainer);

        _toolbar = new ChatToolbar();
        _toolbar.TagInserted += OnTagInserted;
        toolbarContainer.AddChild(_toolbar);

        var separator2 = new HSeparator();
        separator2.CustomMinimumSize = new Vector2(0, 2);
        var sepStyle2 = new StyleBoxFlat();
        sepStyle2.BgColor = StsUiStyles.PanelBorder;
        separator2.AddThemeStyleboxOverride("separator", sepStyle2);
        _mainVBox.AddChild(separator2);

        _contentRow = new HBoxContainer();
        _contentRow.SizeFlagsVertical = SizeFlags.ExpandFill;
        _contentRow.AddThemeConstantOverride("separation", 8);
        var contentPadding = new MarginContainer();
        contentPadding.AddThemeConstantOverride("margin_left", 10);
        contentPadding.AddThemeConstantOverride("margin_right", 10);
        contentPadding.AddThemeConstantOverride("margin_top", 8);
        contentPadding.AddThemeConstantOverride("margin_bottom", 8);
        _mainVBox.AddChild(contentPadding);
        contentPadding.AddChild(_contentRow);

        _lineEdit = new LineEdit();
        _lineEdit.PlaceholderText = LocalizationManager.Instance.GetUI("CHATQAQ-INPUT_PLACEHOLDER");
        _lineEdit.CustomMinimumSize = new Vector2(200, 32);
        _lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _lineEdit.ClearButtonEnabled = true;
        _lineEdit.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        _lineEdit.AddThemeColorOverride("placeholder_color", StsUiStyles.TextMuted);
        _lineEdit.AddThemeColorOverride("caret_color", StsUiStyles.Gold);
        _lineEdit.AddThemeStyleboxOverride("normal", StsUiStyles.CreateInputStyle());
        _lineEdit.AddThemeStyleboxOverride("focus", StsUiStyles.CreateInputStyle());
        _lineEdit.TextChanged += _OnTextChanged;
        _lineEdit.FocusEntered += OnLineEditFocusEntered;
        _lineEdit.FocusExited += OnLineEditFocusExited;
        _contentRow.AddChild(_lineEdit);

        _settingsBtn = StsUiStyles.CreateStsButton("⚙", new Vector2(32, 32));
        _settingsBtn.TooltipText = LocalizationManager.Instance.GetUI("CHATQAQ-SETTINGS");
        _settingsBtn.Pressed += OnSettingsPressed;
        _contentRow.AddChild(_settingsBtn);

        _historyBtn = StsUiStyles.CreateStsButton("📜", new Vector2(32, 32));
        _historyBtn.TooltipText = LocalizationManager.Instance.GetUI("CHATQAQ-HISTORY");
        _historyBtn.Pressed += OnHistoryPressed;
        _contentRow.AddChild(_historyBtn);

        _sendBtn = StsUiStyles.CreateStsButton(LocalizationManager.Instance.GetUI("CHATQAQ-SEND"), new Vector2(60, 32));
        _sendBtn.Pressed += OnSendPressed;
        _contentRow.AddChild(_sendBtn);

        _suggestionPanel = new MentionSuggestionPanel();
        _suggestionPanel.PlayerSelected += OnPlayerSelected;
        AddChild(_suggestionPanel);

        Visible = false;
    }

    public override void _ExitTree()
    {
        HotkeyManager.Instance.OnChatHotkeyPressed -= Toggle;
        base._ExitTree();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    var closeBtnRect = new Rect2(_closeBtn.GlobalPosition, _closeBtn.Size);
                    if (closeBtnRect.HasPoint(mouseButton.GlobalPosition))
                    {
                        return;
                    }

                    var titleRect = new Rect2(_titleBar.GlobalPosition, _titleBar.Size);
                    if (titleRect.HasPoint(mouseButton.GlobalPosition))
                    {
                        _isDragging = true;
                        _dragStartPos = mouseButton.GlobalPosition - GlobalPosition;
                        GetViewport().SetInputAsHandled();
                    }
                }
                else
                {
                    _isDragging = false;
                }
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            _OnDrag(mouseMotion.Relative);
            GetViewport().SetInputAsHandled();
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (_suggestionPanel.IsShowing)
            {
                if (keyEvent.Keycode == Key.Up)
                {
                    _suggestionPanel.NavigateUp();
                    GetViewport().SetInputAsHandled();
                    return;
                }
                else if (keyEvent.Keycode == Key.Down)
                {
                    _suggestionPanel.NavigateDown();
                    GetViewport().SetInputAsHandled();
                    return;
                }
                else if (keyEvent.Keycode == Key.Enter || keyEvent.Keycode == Key.Tab)
                {
                    if (_suggestionPanel.SelectCurrent())
                    {
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                }
                else if (keyEvent.Keycode == Key.Escape)
                {
                    _suggestionPanel.Hide();
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }

            if (keyEvent.Keycode == Key.Enter)
            {
                if (Visible && _lineEdit.HasFocus())
                {
                    OnSendPressed();
                    GetViewport().SetInputAsHandled();
                }
            }
            else if (keyEvent.Keycode == Key.Escape)
            {
                if (Visible && _isMouseInside)
                {
                    OnClosePressed();
                    GetViewport().SetInputAsHandled();
                }
            }
            else
            {
                var config = ConfigManager.Instance.CurrentConfig;
                if (keyEvent.Keycode == config.Hotkey)
                {
                    if (Visible && (_lineEdit.HasFocus() || HotkeyManager.Instance.IsInputFocused))
                    {
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                }
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseEnter)
        {
            _isMouseInside = true;
        }
        else if (what == NotificationMouseExit)
        {
            _isMouseInside = false;
        }
    }

    public new void Show()
    {
        var config = ConfigManager.Instance.CurrentConfig;
        bool hasSavedPosition = config.InputBoxPositionX >= 0 || config.InputBoxPositionY >= 0;

        if (hasSavedPosition)
        {
            SetAnchorsPreset(LayoutPreset.TopLeft);
            var savedPos = new Vector2(config.InputBoxPositionX, config.InputBoxPositionY);
            var viewportSize = GetViewportRect().Size;
            var controlSize = Size;

            savedPos.X = Mathf.Clamp(savedPos.X, 0, viewportSize.X - controlSize.X);
            savedPos.Y = Mathf.Clamp(savedPos.Y, 0, viewportSize.Y - controlSize.Y);

            GlobalPosition = savedPos;

            if (savedPos.X != config.InputBoxPositionX || savedPos.Y != config.InputBoxPositionY)
            {
                config.InputBoxPositionX = savedPos.X;
                config.InputBoxPositionY = savedPos.Y;
                ConfigManager.Instance.Save();
            }
        }
        else
        {
            SetAnchorsPreset(LayoutPreset.Center);
        }

        Visible = true;
        _lineEdit.GrabFocus();
        ChatManager.Instance?.ShowInputBox();

        ChatNetworkManager.Instance?.UpdateOnlinePlayersList();

        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.Out);
        _tween.SetTrans(Tween.TransitionType.Back);
        Modulate = new Color(1, 1, 1, 0);
        Scale = new Vector2(0.9f, 0.9f);
        _tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.15);
        _tween.Parallel().TweenProperty(this, "scale", new Vector2(1, 1), 0.15);
    }

    public new void Hide()
    {
        _suggestionPanel.Hide();

        _tween?.Kill();
        _tween = CreateTween();
        _tween.SetEase(Tween.EaseType.In);
        _tween.SetTrans(Tween.TransitionType.Quad);
        _tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.1);
        _tween.Parallel().TweenProperty(this, "scale", new Vector2(0.95f, 0.95f), 0.1);
        _tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            Scale = Vector2.One;
            Modulate = Colors.White;
            _lineEdit.Text = string.Empty;
            _isMentionMode = false;
            _mentionStartIndex = -1;
            ChatManager.Instance?.HideInputBox();
        }));
    }

    public void Toggle()
    {
        if (Visible)
        {
            _lineEdit.ReleaseFocus();
            HotkeyManager.Instance.IsInputFocused = false;
            Hide();
        }
        else
        {
            Show();
            _lineEdit.GrabFocus();
            HotkeyManager.Instance.IsInputFocused = true;
        }
    }

    public void OnSendPressed()
    {
        var content = _lineEdit.Text.Trim();
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        var convertedContent = BBcodeTagHelper.Instance.ConvertCustomTagsToBbcode(content);
        EmitSignal(SignalName.SendMessage, convertedContent);
        ChatManager.Instance?.SendMessage(content);
        _lineEdit.Text = string.Empty;
    }

    private void OnLineEditFocusEntered()
    {
        HotkeyManager.Instance.IsInputFocused = true;
    }

    private void OnLineEditFocusExited()
    {
        HotkeyManager.Instance.IsInputFocused = false;
    }

    public void OnTagInserted(string tag)
    {
        var currentText = _lineEdit.Text;
        var cursorPos = _lineEdit.CaretColumn;

        var newText = currentText.Substring(0, cursorPos) + tag + currentText.Substring(cursorPos);
        _lineEdit.Text = newText;

        var firstCloseBracket = tag.IndexOf(']');
        if (firstCloseBracket > 0)
        {
            _lineEdit.CaretColumn = cursorPos + firstCloseBracket + 1;
        }
        else
        {
            _lineEdit.CaretColumn = cursorPos + tag.Length;
        }

        _lineEdit.GrabFocus();
    }

    public void OnSettingsPressed()
    {
        EmitSignal(SignalName.OpenSettings);
    }

    public void OnHistoryPressed()
    {
        EmitSignal(SignalName.OpenHistory);
    }

    public void OnClosePressed()
    {
        EmitSignal(SignalName.CloseRequested);
        Hide();
    }

    public void _OnDrag(Vector2 delta)
    {
        var newPos = GlobalPosition + delta;
        var viewportSize = GetViewportRect().Size;
        var controlSize = Size;

        newPos.X = Mathf.Clamp(newPos.X, 0, viewportSize.X - controlSize.X);
        newPos.Y = Mathf.Clamp(newPos.Y, 0, viewportSize.Y - controlSize.Y);

        GlobalPosition = newPos;

        var config = ConfigManager.Instance.CurrentConfig;
        config.InputBoxPositionX = newPos.X;
        config.InputBoxPositionY = newPos.Y;
        ConfigManager.Instance.Save();
    }

    public void _OnTextChanged(string text)
    {
        var currentTime = Time.GetTicksMsec() / 1000.0;

        _isMentionMode = false;
        _mentionStartIndex = -1;

        if (string.IsNullOrEmpty(text))
        {
            _suggestionPanel.Hide();
            return;
        }

        int cursorPos = _lineEdit.CaretColumn;
        int lastAtIndex = text.LastIndexOf('@');

        if (lastAtIndex >= 0 && lastAtIndex < cursorPos)
        {
            string textBeforeCursor = text.Substring(0, cursorPos);
            int searchStart = lastAtIndex + 1;
            string afterAt = textBeforeCursor.Substring(searchStart);

            if (string.IsNullOrEmpty(afterAt) || !afterAt.Contains(" "))
            {
                bool hasSpaceBefore = lastAtIndex == 0 || char.IsWhiteSpace(text[lastAtIndex - 1]);

                if (hasSpaceBefore || lastAtIndex == 0)
                {
                    _isMentionMode = true;
                    _mentionStartIndex = lastAtIndex;
                    _lastInputTime = currentTime;

                    if (currentTime - _lastInputTime >= _debounceDelay || !_suggestionPanel.Visible)
                    {
                        ShowSuggestionPanel(afterAt);
                    }
                    else
                    {
                        _suggestionPanel.UpdateFilter(afterAt);
                    }
                    return;
                }
            }
        }

        _suggestionPanel.Hide();
    }

    private void ShowSuggestionPanel(string filter)
    {
        var lineEditGlobalPos = _lineEdit.GlobalPosition;
        var lineEditSize = _lineEdit.Size;
        var suggestionPos = new Vector2(
            lineEditGlobalPos.X + _lineEdit.CaretColumn * 8,
            lineEditGlobalPos.Y + lineEditSize.Y
        );

        _suggestionPanel.ShowSuggestions(filter, suggestionPos);
    }

    private void OnPlayerSelected(string playerName)
    {
        if (!_isMentionMode || _mentionStartIndex < 0)
        {
            return;
        }

        var currentText = _lineEdit.Text;
        var cursorPos = _lineEdit.CaretColumn;

        string textBeforeMention = currentText.Substring(0, _mentionStartIndex);
        int endIndex = cursorPos;
        for (int i = cursorPos; i < currentText.Length; i++)
        {
            if (currentText[i] == ' ')
            {
                endIndex = i;
                break;
            }
            endIndex = currentText.Length;
        }

        string textAfterMention = currentText.Substring(endIndex);

        string mentionText = playerName.Contains(" ") ? $"@[{playerName}] " : $"@{playerName} ";

        string newText = textBeforeMention + mentionText + textAfterMention;
        _lineEdit.Text = newText;
        _lineEdit.CaretColumn = textBeforeMention.Length + mentionText.Length;

        _isMentionMode = false;
        _mentionStartIndex = -1;
        _suggestionPanel.Hide();

        _lineEdit.GrabFocus();
    }

    public void SetPositionFromConfig()
    {
        var config = ConfigManager.Instance.CurrentConfig;
        GlobalPosition = new Vector2(config.InputBoxPositionX, config.InputBoxPositionY);
    }
}
