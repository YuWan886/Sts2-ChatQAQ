using System.Text.RegularExpressions;
using Godot;
using ChatQAQ.ChatQAQCode.Core;
using ChatQAQ.ChatQAQCode.Data;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Entities.Characters;

namespace ChatQAQ.ChatQAQCode.UI;

public class ItemTagInfo
{
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public CardModel? CardModel { get; set; }
    public PotionModel? PotionModel { get; set; }
    public RelicModel? RelicModel { get; set; }
}

public partial class HistoryPanel : Control
{
    [Signal]
    public delegate void CloseRequestedEventHandler();

    private Panel _backgroundPanel = null!;
    private VBoxContainer _mainContainer = null!;
    private HBoxContainer _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeBtn = null!;
    private HBoxContainer _filterRow = null!;
    private OptionButton _sessionFilter = null!;
    private ScrollContainer _scrollContainer = null!;
    private VBoxContainer _messageList = null!;

    private List<ChatMessage> _messages = new List<ChatMessage>();
    private int _selectedSessionIndex = 0;

    private bool _isDragging = false;
    private Vector2 _dragStartPos;
    private Tween _tween = null!;
    private bool _isMouseInside = false;

    private static readonly Regex ItemTagPattern = new Regex(
        @"\[(card|potion|relic)=([^\]]+)\]([^\[]+)\[/\1\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        CustomMinimumSize = new Vector2(550, 450);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        SetAnchorsPreset(LayoutPreset.Center);
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;

        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundPanel.CustomMinimumSize = new Vector2(550, 450);
        _backgroundPanel.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_backgroundPanel);

        var styleBox = StsUiStyles.CreatePanelStyle(borderRadius: 8, borderWidth: 2, padding: 0);
        _backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);

        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 0);
        _backgroundPanel.AddChild(_mainContainer);

        CreateTitleBar();
        CreateFilterRow();
        CreateMessageList();

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
        _titleLabel.Text = LocalizationManager.Instance.GetUI("CHATQAQ-HISTORY");
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

    private void CreateFilterRow()
    {
        var filterPadding = new MarginContainer();
        filterPadding.AddThemeConstantOverride("margin_left", 12);
        filterPadding.AddThemeConstantOverride("margin_right", 12);
        filterPadding.AddThemeConstantOverride("margin_top", 8);
        filterPadding.AddThemeConstantOverride("margin_bottom", 8);
        _mainContainer.AddChild(filterPadding);

        _filterRow = new HBoxContainer();
        _filterRow.AddThemeConstantOverride("separation", 10);
        filterPadding.AddChild(_filterRow);

        var filterLabel = StsUiStyles.CreateLabel(LocalizationManager.Instance.GetUI("CHATQAQ-SESSION") + ":");
        _filterRow.AddChild(filterLabel);

        _sessionFilter = new OptionButton();
        _sessionFilter.CustomMinimumSize = new Vector2(200, 28);
        _sessionFilter.Text = LocalizationManager.Instance.GetUI("CHATQAQ-ALL_SESSIONS");
        _sessionFilter.ItemSelected += OnSessionSelected;
        StyleOptionButton(_sessionFilter);
        _filterRow.AddChild(_sessionFilter);
    }

    private void CreateMessageList()
    {
        var listPadding = new MarginContainer();
        listPadding.SizeFlagsVertical = SizeFlags.ExpandFill;
        listPadding.AddThemeConstantOverride("margin_left", 12);
        listPadding.AddThemeConstantOverride("margin_right", 12);
        listPadding.AddThemeConstantOverride("margin_bottom", 12);
        _mainContainer.AddChild(listPadding);

        _scrollContainer = new ScrollContainer();
        _scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        var scrollStyle = new StyleBoxFlat();
        scrollStyle.BgColor = new Color(0.04f, 0.04f, 0.06f, 0.8f);
        scrollStyle.SetCornerRadiusAll(4);
        scrollStyle.SetContentMarginAll(4);
        _scrollContainer.AddThemeStyleboxOverride("panel", scrollStyle);
        listPadding.AddChild(_scrollContainer);

        _messageList = new VBoxContainer();
        _messageList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _messageList.AddThemeConstantOverride("separation", 6);
        _scrollContainer.AddChild(_messageList);
    }

    private void StyleOptionButton(OptionButton button)
    {
        var normalStyle = StsUiStyles.CreateInputStyle();
        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        button.AddThemeColorOverride("font_hover_color", StsUiStyles.Gold);
    }

    public new void Show()
    {
        Visible = true;
        RefreshMessages();

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
        }));
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.Escape && _isMouseInside)
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

    public void RefreshMessages()
    {
        foreach (var child in _messageList.GetChildren())
        {
            child.QueueFree();
        }

        UpdateSessionFilter();

        if (_selectedSessionIndex == 0)
        {
            _messages = ChatHistoryManager.Instance.GetAllMessages();
        }
        else if (_selectedSessionIndex > 0 && _selectedSessionIndex <= ChatHistoryManager.Instance.Sessions.Count)
        {
            var session = ChatHistoryManager.Instance.Sessions[_selectedSessionIndex - 1];
            _messages = session.Messages;
        }
        else
        {
            _messages = new List<ChatMessage>();
        }

        foreach (var message in _messages)
        {
            var item = new MessageItem(message);
            _messageList.AddChild(item);
        }
    }

    private void UpdateSessionFilter()
    {
        _sessionFilter.Clear();
        _sessionFilter.AddItem(LocalizationManager.Instance.GetUI("CHATQAQ-ALL_SESSIONS"));

        var sessions = ChatHistoryManager.Instance.Sessions;
        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var label = $"{LocalizationManager.Instance.GetUI("CHATQAQ-SESSION")} {i + 1} - {session.StartTime:yyyy-MM-dd HH:mm}";
            _sessionFilter.AddItem(label);
        }

        if (_selectedSessionIndex >= _sessionFilter.ItemCount)
        {
            _selectedSessionIndex = 0;
        }
        _sessionFilter.Selected = _selectedSessionIndex;
    }

    public void OnSessionSelected(long index)
    {
        _selectedSessionIndex = (int)index;
        RefreshMessages();
    }

    public void OnClosePressed()
    {
        EmitSignal(SignalName.CloseRequested);
        Hide();
    }

    private partial class MessageItem : Panel
    {
        private ChatMessage _message = null!;
        private HBoxContainer _mainHBox = null!;
        private TextureRect _avatar = null!;
        private VBoxContainer _contentContainer = null!;
        private Label _senderLabel = null!;
        private RichTextLabel _messageLabel = null!;
        private HBoxContainer _metaRow = null!;
        private Label _timeLabel = null!;
        private Label _playTimeLabel = null!;
        private List<ItemTagInfo> _itemTags = new();
        private NHoverTipSet? _currentHoverTip;
        private int _currentHoveredTagIndex = -1;
        private CharacterModel? _characterModel;

        private const float HoverTipScale = 0.6f;

        public MessageItem(ChatMessage message)
        {
            _message = message;
            SetupUI();
        }

        private void SetupUI()
        {
            CustomMinimumSize = new Vector2(0, 85);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var styleBox = StsUiStyles.CreateMessageStyle(_message.IsLocalPlayer);
            AddThemeStyleboxOverride("panel", styleBox);

            _mainHBox = new HBoxContainer();
            _mainHBox.SetAnchorsPreset(LayoutPreset.FullRect);
            _mainHBox.AddThemeConstantOverride("separation", 8);
            var padding = new MarginContainer();
            padding.SetAnchorsPreset(LayoutPreset.FullRect);
            padding.AddThemeConstantOverride("margin_left", 8);
            padding.AddThemeConstantOverride("margin_right", 8);
            padding.AddThemeConstantOverride("margin_top", 6);
            padding.AddThemeConstantOverride("margin_bottom", 6);
            AddChild(padding);
            padding.AddChild(_mainHBox);

            _characterModel = FindCharacterById(_message.CharacterId ?? "");

            _contentContainer = new VBoxContainer();
            _contentContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _contentContainer.AddThemeConstantOverride("separation", 4);
            _mainHBox.AddChild(_contentContainer);

            var headerRow = new HBoxContainer();
            headerRow.AddThemeConstantOverride("separation", 6);
            headerRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _contentContainer.AddChild(headerRow);

            if (!_message.IsLocalPlayer)
            {
                var avatarIcon = CreateAvatarIcon();
                headerRow.AddChild(avatarIcon);
            }

            _senderLabel = new Label();
            _senderLabel.Text = _message.SenderName ?? "Unknown";
            _senderLabel.AddThemeColorOverride("font_color", _message.IsLocalPlayer ? StsUiStyles.Blue : StsUiStyles.Orange);
            _senderLabel.AddThemeFontSizeOverride("font_size", 13);
            _senderLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            headerRow.AddChild(_senderLabel);

            if (_characterModel != null)
            {
                var characterNameLabel = new Label();
                try
                {
                    characterNameLabel.Text = _characterModel.Title.GetRawText();
                }
                catch
                {
                    characterNameLabel.Text = _characterModel.Id.Entry;
                }
                characterNameLabel.AddThemeColorOverride("font_color", _characterModel.NameColor);
                characterNameLabel.AddThemeFontSizeOverride("font_size", 12);
                characterNameLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                headerRow.AddChild(characterNameLabel);
            }

            if (_message.IsLocalPlayer)
            {
                var spacer = new Control();
                spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                headerRow.AddChild(spacer);

                var avatarIcon = CreateAvatarIcon();
                headerRow.AddChild(avatarIcon);
            }

            ParseItemTags(_message.Content ?? "");
            _messageLabel = new RichTextLabel();
            _messageLabel.CustomMinimumSize = new Vector2(0, 28);
            _messageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _messageLabel.BbcodeEnabled = true;
            _messageLabel.FitContent = true;
            _messageLabel.ScrollActive = false;
            _messageLabel.AddThemeColorOverride("default_color", StsUiStyles.TextPrimary);
            _messageLabel.AddThemeFontSizeOverride("normal_font_size", 13);
            _contentContainer.AddChild(_messageLabel);

            var displayText = BuildDisplayText(_message.Content ?? "");
            _messageLabel.Text = displayText;
            _messageLabel.MetaHoverStarted += OnMetaHoverStarted;
            _messageLabel.MetaHoverEnded += OnMetaHoverEnded;

            _metaRow = new HBoxContainer();
            _metaRow.AddThemeConstantOverride("separation", 12);
            _contentContainer.AddChild(_metaRow);

            _timeLabel = new Label();
            _timeLabel.Text = _message.Timestamp.ToString("HH:mm:ss");
            _timeLabel.AddThemeColorOverride("font_color", StsUiStyles.TextMuted);
            _timeLabel.AddThemeFontSizeOverride("font_size", 10);
            _metaRow.AddChild(_timeLabel);

            _playTimeLabel = new Label();
            _playTimeLabel.Text = $"Play: {_message.PlayTime:hh\\:mm\\:ss}";
            _playTimeLabel.AddThemeColorOverride("font_color", StsUiStyles.TextMuted);
            _playTimeLabel.AddThemeFontSizeOverride("font_size", 10);
            _metaRow.AddChild(_playTimeLabel);
        }

        private PanelContainer CreateAvatarIcon()
        {
            var avatarIcon = new TextureRect();
            avatarIcon.CustomMinimumSize = new Vector2(28, 28);
            avatarIcon.StretchMode = Godot.TextureRect.StretchModeEnum.KeepAspectCentered;
            avatarIcon.ExpandMode = Godot.TextureRect.ExpandModeEnum.IgnoreSize;
            avatarIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;

            if (_characterModel != null)
            {
                try
                {
                    avatarIcon.Texture = _characterModel.IconTexture;
                }
                catch (Exception ex)
                {
                    MainFile.Logger.Warn($"Failed to load character icon: {ex.Message}");
                }
            }

            var avatarBackground = new PanelContainer();
            avatarBackground.CustomMinimumSize = new Vector2(32, 32);
            avatarBackground.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            var bgStyle = new StyleBoxFlat();
            bgStyle.BgColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
            bgStyle.SetCornerRadiusAll(4);
            bgStyle.SetContentMarginAll(2);
            bgStyle.BorderColor = new Color(0.3f, 0.25f, 0.2f, 1.0f);
            bgStyle.SetBorderWidthAll(1);
            avatarBackground.AddThemeStyleboxOverride("panel", bgStyle);
            avatarBackground.AddChild(avatarIcon);

            return avatarBackground;
        }

        private CharacterModel? FindCharacterById(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                MainFile.Logger.Debug($"FindCharacterById: characterId is null or empty");
                return null;
            }

            MainFile.Logger.Debug($"FindCharacterById: Looking for character '{characterId}'");

            foreach (var character in ModelDb.AllCharacters)
            {
                var idString = character.Id.ToString();
                var idEntry = character.Id.Entry;
                
                MainFile.Logger.Debug($"FindCharacterById: Checking character Id.ToString()='{idString}', Id.Entry='{idEntry}'");
                
                if (string.Equals(idString, characterId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(idEntry, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    MainFile.Logger.Debug($"FindCharacterById: Found matching character '{idEntry}'");
                    return character;
                }
            }
            
            MainFile.Logger.Warn($"FindCharacterById: No character found for id '{characterId}'");
            return null;
        }

        private void ParseItemTags(string text)
        {
            _itemTags.Clear();

            foreach (Match match in ItemTagPattern.Matches(text))
            {
                var type = match.Groups[1].Value.ToLower();
                var id = match.Groups[2].Value;
                var displayName = match.Groups[3].Value;

                var tagInfo = new ItemTagInfo
                {
                    Type = type,
                    Id = id,
                    DisplayName = displayName
                };

                if (type == "card")
                {
                    tagInfo.CardModel = FindCardById(id);
                }
                else if (type == "potion")
                {
                    tagInfo.PotionModel = FindPotionById(id);
                }
                else if (type == "relic")
                {
                    tagInfo.RelicModel = FindRelicById(id);
                }

                _itemTags.Add(tagInfo);
            }
        }

        private string BuildDisplayText(string text)
        {
            var result = text;
            var matches = ItemTagPattern.Matches(text);

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                var type = match.Groups[1].Value.ToLower();
                var displayName = match.Groups[3].Value;
                var prefix = type switch
                {
                    "card" => "\U0001F0CF ",
                    "potion" => "\U0001F9EA ",
                    "relic" => "\U0001F48E ",
                    _ => ""
                };

                var newTag = $"[url={i}]{prefix}{displayName}[/url]";
                result = result.Remove(match.Index, match.Length).Insert(match.Index, newTag);
            }

            return $"[center]{result}[/center]";
        }

        private void OnMetaHoverStarted(Variant meta)
        {
            if (!int.TryParse(meta.ToString(), out var tagIndex)) return;
            if (tagIndex < 0 || tagIndex >= _itemTags.Count) return;

            _currentHoveredTagIndex = tagIndex;
            ShowHoverTipForTag(tagIndex);
        }

        private void OnMetaHoverEnded(Variant meta)
        {
            ClearHoverTip();
            _currentHoveredTagIndex = -1;
        }

        private void ShowHoverTipForTag(int index)
        {
            if (_messageLabel == null || index < 0 || index >= _itemTags.Count) return;

            ClearHoverTip();

            var tag = _itemTags[index];
            try
            {
                if (tag.Type == "card" && tag.CardModel != null)
                {
                    var hoverTip = HoverTipFactory.FromCard(tag.CardModel);
                    _currentHoverTip = NHoverTipSet.CreateAndShow(_messageLabel, hoverTip, HoverTipAlignment.Center);
                }
                else if (tag.Type == "potion" && tag.PotionModel != null)
                {
                    var hoverTip = HoverTipFactory.FromPotion(tag.PotionModel);
                    _currentHoverTip = NHoverTipSet.CreateAndShow(_messageLabel, hoverTip, HoverTipAlignment.Center);
                }
                else if (tag.Type == "relic" && tag.RelicModel != null)
                {
                    var hoverTips = HoverTipFactory.FromRelic(tag.RelicModel);
                    _currentHoverTip = NHoverTipSet.CreateAndShow(_messageLabel, hoverTips, HoverTipAlignment.Center);
                }

                if (_currentHoverTip != null)
                {
                    _currentHoverTip.Scale = new Vector2(HoverTipScale, HoverTipScale);
                    _currentHoverTip.ZIndex = 1000;

                    var mousePos = GetGlobalMousePosition();
                    _currentHoverTip.GlobalPosition = new Vector2(mousePos.X + 10, mousePos.Y - _currentHoverTip.Size.Y - 10);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"Failed to show hover tip: {ex.Message}");
            }
        }

        private void ClearHoverTip()
        {
            if (_currentHoverTip != null && _messageLabel != null)
            {
                try
                {
                    NHoverTipSet.Remove(_messageLabel);
                }
                catch { }
                _currentHoverTip = null;
            }
        }

        private CardModel? FindCardById(string id)
        {
            foreach (var card in ModelDb.AllCards)
            {
                if (string.Equals(card.Id.ToString(), id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(card.Id.Entry, id, StringComparison.OrdinalIgnoreCase))
                {
                    return card;
                }
            }
            return null;
        }

        private PotionModel? FindPotionById(string id)
        {
            foreach (var potion in ModelDb.AllPotions)
            {
                if (string.Equals(potion.Id.ToString(), id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(potion.Id.Entry, id, StringComparison.OrdinalIgnoreCase))
                {
                    return potion;
                }
            }
            return null;
        }

        private RelicModel? FindRelicById(string id)
        {
            foreach (var relic in ModelDb.AllRelics)
            {
                if (string.Equals(relic.Id.ToString(), id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(relic.Id.Entry, id, StringComparison.OrdinalIgnoreCase))
                {
                    return relic;
                }
            }
            return null;
        }
    }
}
