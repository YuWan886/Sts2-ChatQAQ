using Godot;
using ChatQAQ.ChatQAQCode.Core;
using ChatQAQ.ChatQAQCode.Data;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class SettingsPanel : Control
{
    private Panel _backgroundPanel = null!;
    private VBoxContainer _mainContainer = null!;
    private HBoxContainer _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeBtn = null!;

    private VBoxContainer _settingsContainer = null!;
    private HBoxContainer _hotkeyRow = null!;
    private HBoxContainer _durationRow = null!;
    private HBoxContainer _maxHistoryRow = null!;
    private HBoxContainer _mentionSoundRow = null!;
    private HBoxContainer _mentionNotificationRow = null!;
    private HBoxContainer _buttonRow = null!;
    private Button _resetBtn = null!;

    private Label _hotkeyLabel = null!;
    private Button _hotkeyBtn = null!;
    private Label _durationLabel = null!;
    private SpinBox _durationSpinBox = null!;
    private Label _maxHistoryLabel = null!;
    private SpinBox _maxHistorySpinBox = null!;
    private Label _mentionSoundLabel = null!;
    private CheckButton _mentionSoundCheck = null!;
    private Label _mentionNotificationLabel = null!;
    private CheckButton _mentionNotificationCheck = null!;

    private bool _isWaitingForHotkey = false;
    private bool _isDragging = false;
    private Vector2 _dragStartPos;
    private Tween _tween = null!;
    private bool _isMouseInside = false;

    [Signal]
    public delegate void SettingsChangedEventHandler();

    [Signal]
    public delegate void CloseRequestedEventHandler();

    public override void _Ready()
    {
        OnReady();
    }

    private void OnReady()
    {
        CustomMinimumSize = new Vector2(400, 380);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        SetAnchorsPreset(LayoutPreset.Center);
        MouseFilter = MouseFilterEnum.Stop;

        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundPanel.CustomMinimumSize = new Vector2(400, 380);
        _backgroundPanel.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_backgroundPanel);

        var styleBox = StsUiStyles.CreatePanelStyle(borderRadius: 8, borderWidth: 2, padding: 0);
        _backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);

        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 0);
        _backgroundPanel.AddChild(_mainContainer);

        CreateTitleBar();

        var contentPadding = new MarginContainer();
        contentPadding.SizeFlagsVertical = SizeFlags.ExpandFill;
        contentPadding.AddThemeConstantOverride("margin_left", 16);
        contentPadding.AddThemeConstantOverride("margin_right", 16);
        contentPadding.AddThemeConstantOverride("margin_top", 12);
        contentPadding.AddThemeConstantOverride("margin_bottom", 12);
        _mainContainer.AddChild(contentPadding);

        _settingsContainer = new VBoxContainer();
        _settingsContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _settingsContainer.AddThemeConstantOverride("separation", 12);
        contentPadding.AddChild(_settingsContainer);

        CreateHotkeyRow();
        CreateDurationRow();
        CreateMaxHistoryRow();
        CreateMentionSoundRow();
        CreateMentionNotificationRow();
        CreateButtons();

        LoadCurrentSettings();

        Visible = false;
    }

    private void CreateTitleBar()
    {
        _titleBar = new HBoxContainer();
        _titleBar.CustomMinimumSize = new Vector2(0, 32);
        _titleBar.AddThemeConstantOverride("separation", 8);
        var titleBarStyle = new StyleBoxFlat();
        titleBarStyle.BgColor = new Color(0.08f, 0.06f, 0.04f, 1.0f);
        titleBarStyle.SetContentMarginAll(8);
        _titleBar.AddThemeStyleboxOverride("panel", titleBarStyle);
        _mainContainer.AddChild(_titleBar);

        var dragHint = new Label();
        dragHint.Text = "⋮⋮";
        dragHint.AddThemeColorOverride("font_color", StsUiStyles.TextMuted);
        dragHint.AddThemeFontSizeOverride("font_size", 12);
        dragHint.MouseFilter = MouseFilterEnum.Pass;
        _titleBar.AddChild(dragHint);

        _titleLabel = new Label();
        _titleLabel.Text = LocalizationManager.Instance.GetUI("CHATQAQ-SETTINGS");
        _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.VerticalAlignment = VerticalAlignment.Center;
        _titleLabel.AddThemeColorOverride("font_color", StsUiStyles.Gold);
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.MouseFilter = MouseFilterEnum.Pass;
        _titleBar.AddChild(_titleLabel);

        _closeBtn = StsUiStyles.CreateCloseButton();
        _closeBtn.Pressed += OnClosePressed;
        _titleBar.AddChild(_closeBtn);

        var separator = new HSeparator();
        separator.CustomMinimumSize = new Vector2(0, 2);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = StsUiStyles.PanelBorder;
        separator.AddThemeStyleboxOverride("separator", sepStyle);
        _mainContainer.AddChild(separator);
    }

    private void CreateHotkeyRow()
    {
        _hotkeyRow = CreateRowContainer();

        _hotkeyLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-HOTKEY_LABEL") + ":");
        _hotkeyLabel.CustomMinimumSize = new Vector2(180, 28);

        _hotkeyBtn = StsUiStyles.CreateStsButton("", new Vector2(100, 28));
        _hotkeyBtn.Pressed += OnHotkeyButtonPressed;

        _hotkeyRow.AddChild(_hotkeyLabel);
        _hotkeyRow.AddChild(_hotkeyBtn);
        _settingsContainer.AddChild(_hotkeyRow);
    }

    private void CreateDurationRow()
    {
        _durationRow = CreateRowContainer();

        _durationLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-DURATION_LABEL") + ":");
        _durationLabel.CustomMinimumSize = new Vector2(180, 28);

        _durationSpinBox = new SpinBox();
        _durationSpinBox.CustomMinimumSize = new Vector2(100, 28);
        _durationSpinBox.MinValue = 0.5;
        _durationSpinBox.MaxValue = 30.0;
        _durationSpinBox.Step = 0.5;
        _durationSpinBox.ValueChanged += OnDurationChanged;
        StyleSpinBox(_durationSpinBox);

        _durationRow.AddChild(_durationLabel);
        _durationRow.AddChild(_durationSpinBox);
        _settingsContainer.AddChild(_durationRow);
    }

    private void CreateMaxHistoryRow()
    {
        _maxHistoryRow = CreateRowContainer();

        _maxHistoryLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-MAX_HISTORY_LABEL") + ":");
        _maxHistoryLabel.CustomMinimumSize = new Vector2(180, 28);

        _maxHistorySpinBox = new SpinBox();
        _maxHistorySpinBox.CustomMinimumSize = new Vector2(100, 28);
        _maxHistorySpinBox.MinValue = 100;
        _maxHistorySpinBox.MaxValue = 10000;
        _maxHistorySpinBox.Step = 100;
        _maxHistorySpinBox.ValueChanged += OnMaxHistoryChanged;
        StyleSpinBox(_maxHistorySpinBox);

        _maxHistoryRow.AddChild(_maxHistoryLabel);
        _maxHistoryRow.AddChild(_maxHistorySpinBox);
        _settingsContainer.AddChild(_maxHistoryRow);
    }

    private void CreateMentionSoundRow()
    {
        _mentionSoundRow = CreateRowContainer();

        _mentionSoundLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-MENTION_SOUND_LABEL") + ":");
        _mentionSoundLabel.CustomMinimumSize = new Vector2(180, 28);

        _mentionSoundCheck = new CheckButton();
        _mentionSoundCheck.CustomMinimumSize = new Vector2(60, 28);
        _mentionSoundCheck.Toggled += OnMentionSoundToggled;
        StyleCheckButton(_mentionSoundCheck);

        _mentionSoundRow.AddChild(_mentionSoundLabel);
        _mentionSoundRow.AddChild(_mentionSoundCheck);
        _settingsContainer.AddChild(_mentionSoundRow);
    }

    private void CreateMentionNotificationRow()
    {
        _mentionNotificationRow = CreateRowContainer();

        _mentionNotificationLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-MENTION_NOTIFICATION_LABEL") + ":");
        _mentionNotificationLabel.CustomMinimumSize = new Vector2(180, 28);

        _mentionNotificationCheck = new CheckButton();
        _mentionNotificationCheck.CustomMinimumSize = new Vector2(60, 28);
        _mentionNotificationCheck.Toggled += OnMentionNotificationToggled;
        StyleCheckButton(_mentionNotificationCheck);

        _mentionNotificationRow.AddChild(_mentionNotificationLabel);
        _mentionNotificationRow.AddChild(_mentionNotificationCheck);
        _settingsContainer.AddChild(_mentionNotificationRow);
    }

    private void CreateButtons()
    {
        var spacer = new Control();
        spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        _settingsContainer.AddChild(spacer);

        _buttonRow = new HBoxContainer();
        _buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        _buttonRow.AddThemeConstantOverride("separation", 16);
        _settingsContainer.AddChild(_buttonRow);

        _resetBtn = StsUiStyles.CreateStsButton(LocalizationManager.Instance.GetUI("CHATQAQ-RESET_DEFAULTS"), new Vector2(120, 32));
        _resetBtn.Pressed += OnResetDefaultsPressed;
        _buttonRow.AddChild(_resetBtn);
    }

    private HBoxContainer CreateRowContainer()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        return row;
    }

    private void StyleSpinBox(SpinBox spinBox)
    {
        spinBox.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        var lineEdit = spinBox.GetLineEdit();
        if (lineEdit != null)
        {
            lineEdit.AddThemeStyleboxOverride("normal", StsUiStyles.CreateInputStyle());
            lineEdit.AddThemeStyleboxOverride("focus", StsUiStyles.CreateInputStyle());
        }
    }

    private void StyleCheckButton(CheckButton checkButton)
    {
        checkButton.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        checkButton.AddThemeColorOverride("font_hover_color", StsUiStyles.Gold);
    }

    private void LoadCurrentSettings()
    {
        var config = ConfigManager.Instance.CurrentConfig;

        _hotkeyBtn.Text = config.Hotkey.ToString();
        _durationSpinBox.Value = config.BubbleDisplayDuration;
        _maxHistorySpinBox.Value = config.MaxHistoryMessages;
        _mentionSoundCheck.ButtonPressed = config.EnableMentionSound;
        _mentionNotificationCheck.ButtonPressed = config.EnableMentionNotification;
    }

    public new void Show()
    {
        Visible = true;
        LoadCurrentSettings();

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
            _isWaitingForHotkey = false;
        }));
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        
        if (_isWaitingForHotkey && @event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            OnHotkeyChanged(keyEvent.Keycode);
            _isWaitingForHotkey = false;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey escEvent && escEvent.Pressed && !escEvent.Echo)
        {
            if (escEvent.Keycode == Key.Escape && _isMouseInside)
            {
                OnClosePressed();
                GetViewport().SetInputAsHandled();
                return;
            }
        }

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
            var newPos = GlobalPosition + mouseMotion.Relative;
            var viewportSize = GetViewportRect().Size;
            var controlSize = Size;

            newPos.X = Mathf.Clamp(newPos.X, 0, viewportSize.X - controlSize.X);
            newPos.Y = Mathf.Clamp(newPos.Y, 0, viewportSize.Y - controlSize.Y);

            GlobalPosition = newPos;
            GetViewport().SetInputAsHandled();
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

    private void OnHotkeyButtonPressed()
    {
        _isWaitingForHotkey = true;
        _hotkeyBtn.Text = "...";
    }

    private void OnHotkeyChanged(Key newKey)
    {
        ConfigManager.Instance.CurrentConfig.Hotkey = newKey;
        ConfigManager.Instance.Save();
        _hotkeyBtn.Text = newKey.ToString();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnDurationChanged(double value)
    {
        ConfigManager.Instance.CurrentConfig.BubbleDisplayDuration = (float)value;
        ConfigManager.Instance.Save();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnMaxHistoryChanged(double value)
    {
        ConfigManager.Instance.CurrentConfig.MaxHistoryMessages = (int)value;
        ConfigManager.Instance.Save();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnMentionSoundToggled(bool enabled)
    {
        ConfigManager.Instance.CurrentConfig.EnableMentionSound = enabled;
        ConfigManager.Instance.Save();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnMentionNotificationToggled(bool enabled)
    {
        ConfigManager.Instance.CurrentConfig.EnableMentionNotification = enabled;
        ConfigManager.Instance.Save();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnResetDefaultsPressed()
    {
        ConfigManager.Instance.ResetToDefault();
        LoadCurrentSettings();
        EmitSignal(SignalName.SettingsChanged);
    }

    private void OnClosePressed()
    {
        EmitSignal(SignalName.CloseRequested);
        Hide();
    }
}
