using Godot;
using ChatQAQ.ChatQAQCode.Core;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class ChatToolbar : Control
{
    [Signal]
    public delegate void TagInsertedEventHandler(string tag);

    [Signal]
    public delegate void ItemSearchRequestedEventHandler(string itemType, string searchTerm);

    private HBoxContainer _mainContainer = null!;
    private Button _colorBtn = null!;
    private Button _boldBtn = null!;
    private Button _italicBtn = null!;
    private Button _underlineBtn = null!;
    private Button _cardBtn = null!;
    private Button _potionBtn = null!;
    private Button _relicBtn = null!;
    private Button _emojiBtn = null!;
    private PopupMenu _colorPopup = null!;
    private PopupPanel _itemSearchPopup = null!;
    private PopupPanel _emojiPopup = null!;
    private LineEdit _itemSearchInput = null!;
    private ItemList _itemSearchList = null!;
    private Label _itemSearchTitle = null!;
    private GridContainer _emojiGrid = null!;

    private BBcodeTagHelper.TagType _currentSearchType;
    private List<ItemSearchResult> _searchResults = new();
    private NHoverTipSet? _currentHoverTip;
    private int _lastHoveredIndex = -1;

    private class ItemSearchResult
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public ItemReferenceSystem.ItemType Type { get; set; }
        public CardModel? CardModel { get; set; }
        public PotionModel? PotionModel { get; set; }
        public RelicModel? RelicModel { get; set; }
    }

    private class EmojiInfo
    {
        public string Emoji { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        CustomMinimumSize = new Vector2(0, 36);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Pass;

        _mainContainer = new HBoxContainer();
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 4);
        AddChild(_mainContainer);

        var separatorLeft = new VSeparator();
        separatorLeft.CustomMinimumSize = new Vector2(2, 24);
        _mainContainer.AddChild(separatorLeft);

        _colorBtn = CreateToolbarButton("🎨", "color");
        _colorBtn.Pressed += OnColorPressed;
        _mainContainer.AddChild(_colorBtn);

        _boldBtn = CreateToolbarButton("B", "bold");
        _boldBtn.Pressed += () => EmitSignal(SignalName.TagInserted, "[b][/b]");
        _mainContainer.AddChild(_boldBtn);

        _italicBtn = CreateToolbarButton("I", "italic");
        _italicBtn.Pressed += () => EmitSignal(SignalName.TagInserted, "[i][/i]");
        _mainContainer.AddChild(_italicBtn);

        _underlineBtn = CreateToolbarButton("U", "underline");
        _underlineBtn.Pressed += () => EmitSignal(SignalName.TagInserted, "[u][/u]");
        _mainContainer.AddChild(_underlineBtn);

        var separatorMiddle = new VSeparator();
        separatorMiddle.CustomMinimumSize = new Vector2(2, 24);
        _mainContainer.AddChild(separatorMiddle);

        _cardBtn = CreateToolbarButton("🃏", "card");
        _cardBtn.Pressed += () => OnItemSearchRequested(BBcodeTagHelper.TagType.Card);
        _mainContainer.AddChild(_cardBtn);

        _potionBtn = CreateToolbarButton("🧪", "potion");
        _potionBtn.Pressed += () => OnItemSearchRequested(BBcodeTagHelper.TagType.Potion);
        _mainContainer.AddChild(_potionBtn);

        _relicBtn = CreateToolbarButton("💎", "relic");
        _relicBtn.Pressed += () => OnItemSearchRequested(BBcodeTagHelper.TagType.Relic);
        _mainContainer.AddChild(_relicBtn);

        var separatorRight = new VSeparator();
        separatorRight.CustomMinimumSize = new Vector2(2, 24);
        _mainContainer.AddChild(separatorRight);

        _emojiBtn = CreateToolbarButton("😊", "emoji");
        _emojiBtn.Pressed += OnEmojiBtnPressed;
        _mainContainer.AddChild(_emojiBtn);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _mainContainer.AddChild(spacer);

        SetupColorPopup();
        SetupItemSearchPopup();
    }

    private Button CreateToolbarButton(string text, string tooltipKey)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(32, 28);
        btn.TooltipText = LocalizationManager.Instance.GetUI($"CHATQAQ-TAG_{tooltipKey.ToUpper()}");
        
        var styleNormal = new StyleBoxFlat();
        styleNormal.BgColor = new Color(0.15f, 0.12f, 0.1f, 0.8f);
        styleNormal.SetBorderWidthAll(1);
        styleNormal.BorderColor = new Color(0.3f, 0.25f, 0.2f);
        styleNormal.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", styleNormal);

        var styleHover = new StyleBoxFlat();
        styleHover.BgColor = new Color(0.25f, 0.2f, 0.15f, 0.9f);
        styleHover.SetBorderWidthAll(1);
        styleHover.BorderColor = StsUiStyles.Gold;
        styleHover.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", styleHover);

        var stylePressed = new StyleBoxFlat();
        stylePressed.BgColor = new Color(0.3f, 0.25f, 0.2f, 1.0f);
        stylePressed.SetBorderWidthAll(1);
        stylePressed.BorderColor = StsUiStyles.Gold;
        stylePressed.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("pressed", stylePressed);

        btn.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", StsUiStyles.Gold);
        btn.AddThemeFontSizeOverride("font_size", 14);

        return btn;
    }

    private void SetupColorPopup()
    {
        _colorPopup = new PopupMenu();
        _colorPopup.IdPressed += OnColorSelected;
        AddChild(_colorPopup);

        var colors = new[]
        {
            ("red", "FF4444"),
            ("green", "44FF44"),
            ("blue", "4444FF"),
            ("yellow", "FFFF44"),
            ("cyan", "44FFFF"),
            ("magenta", "FF44FF"),
            ("orange", "FF8844"),
            ("purple", "8844FF"),
            ("gold", "FFD700"),
            ("white", "FFFFFF")
        };

        foreach (var (name, hex) in colors)
        {
            var color = new Color(hex);
            _colorPopup.AddItem($"[color={name}] ■ [/color]", _colorPopup.ItemCount);
        }
    }

    private void SetupItemSearchPopup()
    {
        _itemSearchPopup = new PopupPanel();
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.08f, 0.06f, 0.95f);
        style.SetBorderWidthAll(2);
        style.BorderColor = StsUiStyles.PanelBorder;
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(10);
        _itemSearchPopup.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(300, 400);
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);
        _itemSearchPopup.AddChild(vbox);

        _itemSearchTitle = new Label();
        _itemSearchTitle.Text = LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_ITEMS");
        _itemSearchTitle.AddThemeColorOverride("font_color", StsUiStyles.Gold);
        _itemSearchTitle.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(_itemSearchTitle);

        _itemSearchInput = new LineEdit();
        _itemSearchInput.PlaceholderText = LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_PLACEHOLDER");
        _itemSearchInput.CustomMinimumSize = new Vector2(0, 32);
        _itemSearchInput.TextChanged += OnSearchTextChanged;
        _itemSearchInput.AddThemeStyleboxOverride("normal", StsUiStyles.CreateInputStyle());
        vbox.AddChild(_itemSearchInput);

        _itemSearchList = new ItemList();
        _itemSearchList.SizeFlagsVertical = SizeFlags.ExpandFill;
        _itemSearchList.ItemSelected += OnItemSelected;
        _itemSearchList.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        _itemSearchList.AddThemeColorOverride("font_selected_color", StsUiStyles.Gold);
        vbox.AddChild(_itemSearchList);

        AddChild(_itemSearchPopup);
    }

    private void SetupEmojiPopup()
    {
        _emojiPopup = new PopupPanel();
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.08f, 0.06f, 0.95f);
        style.SetBorderWidthAll(2);
        style.BorderColor = StsUiStyles.PanelBorder;
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(10);
        _emojiPopup.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(320, 280);
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);
        _emojiPopup.AddChild(vbox);

        var titleLabel = new Label();
        titleLabel.Text = LocalizationManager.Instance.GetUI("CHATQAQ-EMOJI_PICKER");
        titleLabel.AddThemeColorOverride("font_color", StsUiStyles.Gold);
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(titleLabel);

        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        scrollContainer.CustomMinimumSize = new Vector2(0, 200);
        vbox.AddChild(scrollContainer);

        _emojiGrid = new GridContainer();
        _emojiGrid.Columns = 8;
        _emojiGrid.AddThemeConstantOverride("h_separation", 4);
        _emojiGrid.AddThemeConstantOverride("v_separation", 4);
        scrollContainer.AddChild(_emojiGrid);

        PopulateEmojiGrid();

        AddChild(_emojiPopup);
    }

    private void PopulateEmojiGrid()
    {
        while (_emojiGrid.GetChildCount() > 0)
        {
            _emojiGrid.GetChild(0).QueueFree();
        }

        var emojis = GetCommonEmojis();
        
        foreach (var emoji in emojis)
        {
            var emojiBtn = CreateEmojiButton(emoji);
            _emojiGrid.AddChild(emojiBtn);
        }
    }

    private Button CreateEmojiButton(EmojiInfo emojiInfo)
    {
        var btn = new Button();
        btn.Text = emojiInfo.Emoji;
        btn.CustomMinimumSize = new Vector2(36, 36);
        btn.TooltipText = emojiInfo.Name;

        var styleNormal = new StyleBoxFlat();
        styleNormal.BgColor = new Color(0.12f, 0.1f, 0.08f, 0.8f);
        styleNormal.SetBorderWidthAll(1);
        styleNormal.BorderColor = new Color(0.25f, 0.2f, 0.15f);
        styleNormal.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("normal", styleNormal);

        var styleHover = new StyleBoxFlat();
        styleHover.BgColor = new Color(0.2f, 0.15f, 0.1f, 0.9f);
        styleHover.SetBorderWidthAll(1);
        styleHover.BorderColor = StsUiStyles.Gold;
        styleHover.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("hover", styleHover);

        var stylePressed = new StyleBoxFlat();
        stylePressed.BgColor = new Color(0.3f, 0.25f, 0.2f, 1.0f);
        stylePressed.SetBorderWidthAll(1);
        stylePressed.BorderColor = StsUiStyles.Gold;
        stylePressed.SetCornerRadiusAll(4);
        btn.AddThemeStyleboxOverride("pressed", stylePressed);

        btn.AddThemeColorOverride("font_color", Colors.White);
        btn.AddThemeColorOverride("font_hover_color", StsUiStyles.Gold);
        btn.AddThemeFontSizeOverride("font_size", 18);

        btn.Pressed += () => OnEmojiBtnSelected(emojiInfo.Emoji);

        return btn;
    }

    private List<EmojiInfo> GetCommonEmojis()
    {
        return new List<EmojiInfo>
        {
            new EmojiInfo { Emoji = "😀", Name = "Grinning Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "😂", Name = "Face with Tears of Joy", Category = "Smileys" },
            new EmojiInfo { Emoji = "😊", Name = "Smiling Face with Smiling Eyes", Category = "Smileys" },
            new EmojiInfo { Emoji = "😍", Name = "Smiling Face with Heart-Eyes", Category = "Smileys" },
            new EmojiInfo { Emoji = "🤣", Name = "Rolling on the Floor Laughing", Category = "Smileys" },
            new EmojiInfo { Emoji = "😎", Name = "Smiling Face with Sunglasses", Category = "Smileys" },
            new EmojiInfo { Emoji = "🤔", Name = "Thinking Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "😴", Name = "Sleeping Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "🥳", Name = "Partying Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "😢", Name = "Crying Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "😡", Name = "Pouting Face", Category = "Smileys" },
            new EmojiInfo { Emoji = "😱", Name = "Face Screaming in Fear", Category = "Smileys" },
            new EmojiInfo { Emoji = "👍", Name = "Thumbs Up", Category = "Gestures" },
            new EmojiInfo { Emoji = "👎", Name = "Thumbs Down", Category = "Gestures" },
            new EmojiInfo { Emoji = "👏", Name = "Clapping Hands", Category = "Gestures" },
            new EmojiInfo { Emoji = "🙏", Name = "Folded Hands", Category = "Gestures" },
            new EmojiInfo { Emoji = "💪", Name = "Flexed Biceps", Category = "Gestures" },
            new EmojiInfo { Emoji = "🤝", Name = "Handshake", Category = "Gestures" },
            new EmojiInfo { Emoji = "✌️", Name = "Victory Hand", Category = "Gestures" },
            new EmojiInfo { Emoji = "👋", Name = "Waving Hand", Category = "Gestures" },
            new EmojiInfo { Emoji = "❤️", Name = "Red Heart", Category = "Symbols" },
            new EmojiInfo { Emoji = "💔", Name = "Broken Heart", Category = "Symbols" },
            new EmojiInfo { Emoji = "💯", Name = "Hundred Points", Category = "Symbols" },
            new EmojiInfo { Emoji = "⭐", Name = "Star", Category = "Symbols" },
            new EmojiInfo { Emoji = "🔥", Name = "Fire", Category = "Symbols" },
            new EmojiInfo { Emoji = "💎", Name = "Gem Stone", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎉", Name = "Party Popper", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎮", Name = "Video Game", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎯", Name = "Direct Hit", Category = "Symbols" },
            new EmojiInfo { Emoji = "🏆", Name = "Trophy", Category = "Symbols" },
            new EmojiInfo { Emoji = "💰", Name = "Money Bag", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎲", Name = "Game Die", Category = "Symbols" },
            new EmojiInfo { Emoji = "🃏", Name = "Joker", Category = "Symbols" },
            new EmojiInfo { Emoji = "🧪", Name = "Test Tube", Category = "Symbols" },
            new EmojiInfo { Emoji = "⚔️", Name = "Crossed Swords", Category = "Symbols" },
            new EmojiInfo { Emoji = "🛡️", Name = "Shield", Category = "Symbols" },
            new EmojiInfo { Emoji = "💀", Name = "Skull", Category = "Symbols" },
            new EmojiInfo { Emoji = "👻", Name = "Ghost", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎁", Name = "Wrapped Gift", Category = "Symbols" },
            new EmojiInfo { Emoji = "✨", Name = "Sparkles", Category = "Symbols" },
            new EmojiInfo { Emoji = "🌟", Name = "Glowing Star", Category = "Symbols" },
            new EmojiInfo { Emoji = "💫", Name = "Dizzy", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎵", Name = "Musical Note", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎶", Name = "Musical Notes", Category = "Symbols" },
            new EmojiInfo { Emoji = "📢", Name = "Loudspeaker", Category = "Symbols" },
            new EmojiInfo { Emoji = "💬", Name = "Speech Balloon", Category = "Symbols" },
            new EmojiInfo { Emoji = "📝", Name = "Memo", Category = "Symbols" },
            new EmojiInfo { Emoji = "🔔", Name = "Bell", Category = "Symbols" },
            new EmojiInfo { Emoji = "⚡", Name = "High Voltage", Category = "Symbols" },
            new EmojiInfo { Emoji = "🌈", Name = "Rainbow", Category = "Symbols" },
            new EmojiInfo { Emoji = "☀️", Name = "Sun", Category = "Symbols" },
            new EmojiInfo { Emoji = "🌙", Name = "Crescent Moon", Category = "Symbols" },
            new EmojiInfo { Emoji = "❄️", Name = "Snowflake", Category = "Symbols" },
            new EmojiInfo { Emoji = "🌊", Name = "Water Wave", Category = "Symbols" },
            new EmojiInfo { Emoji = "🍀", Name = "Four Leaf Clover", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎲", Name = "Game Die", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎪", Name = "Circus Tent", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎭", Name = "Performing Arts", Category = "Symbols" },
            new EmojiInfo { Emoji = "🎨", Name = "Artist Palette", Category = "Symbols" },
            new EmojiInfo { Emoji = "📚", Name = "Books", Category = "Symbols" },
            new EmojiInfo { Emoji = "🔮", Name = "Crystal Ball", Category = "Symbols" },
            new EmojiInfo { Emoji = "🧙", Name = "Mage", Category = "Symbols" },
            new EmojiInfo { Emoji = "🧝", Name = "Elf", Category = "Symbols" },
            new EmojiInfo { Emoji = "🐉", Name = "Dragon", Category = "Animals" },
            new EmojiInfo { Emoji = "🦄", Name = "Unicorn", Category = "Animals" },
            new EmojiInfo { Emoji = "🐺", Name = "Wolf", Category = "Animals" },
            new EmojiInfo { Emoji = "🦊", Name = "Fox", Category = "Animals" },
            new EmojiInfo { Emoji = "🐱", Name = "Cat Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐶", Name = "Dog Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐸", Name = "Frog", Category = "Animals" },
            new EmojiInfo { Emoji = "🦋", Name = "Butterfly", Category = "Animals" },
            new EmojiInfo { Emoji = "🐦", Name = "Bird", Category = "Animals" },
            new EmojiInfo { Emoji = "🦅", Name = "Eagle", Category = "Animals" },
            new EmojiInfo { Emoji = "🐙", Name = "Octopus", Category = "Animals" },
            new EmojiInfo { Emoji = "🦑", Name = "Squid", Category = "Animals" },
            new EmojiInfo { Emoji = "🐢", Name = "Turtle", Category = "Animals" },
            new EmojiInfo { Emoji = "🦈", Name = "Shark", Category = "Animals" },
            new EmojiInfo { Emoji = "🐍", Name = "Snake", Category = "Animals" },
            new EmojiInfo { Emoji = "🦎", Name = "Lizard", Category = "Animals" },
            new EmojiInfo { Emoji = "🦂", Name = "Scorpion", Category = "Animals" },
            new EmojiInfo { Emoji = "🕷️", Name = "Spider", Category = "Animals" },
            new EmojiInfo { Emoji = "🐛", Name = "Bug", Category = "Animals" },
            new EmojiInfo { Emoji = "🦗", Name = "Cricket", Category = "Animals" },
            new EmojiInfo { Emoji = "🐜", Name = "Ant", Category = "Animals" },
            new EmojiInfo { Emoji = "🐝", Name = "Honeybee", Category = "Animals" },
            new EmojiInfo { Emoji = "🦟", Name = "Mosquito", Category = "Animals" },
            new EmojiInfo { Emoji = "🦠", Name = "Microbe", Category = "Animals" },
            new EmojiInfo { Emoji = "🐲", Name = "Dragon Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🦇", Name = "Bat", Category = "Animals" },
            new EmojiInfo { Emoji = "🦉", Name = "Owl", Category = "Animals" },
            new EmojiInfo { Emoji = "🐺", Name = "Wolf", Category = "Animals" },
            new EmojiInfo { Emoji = "🦌", Name = "Deer", Category = "Animals" },
            new EmojiInfo { Emoji = "🐗", Name = "Boar", Category = "Animals" },
            new EmojiInfo { Emoji = "🐿️", Name = "Chipmunk", Category = "Animals" },
            new EmojiInfo { Emoji = "🦔", Name = "Hedgehog", Category = "Animals" },
            new EmojiInfo { Emoji = "🐰", Name = "Rabbit Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐹", Name = "Hamster Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐭", Name = "Mouse Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐀", Name = "Rat", Category = "Animals" },
            new EmojiInfo { Emoji = "🐁", Name = "Mouse", Category = "Animals" },
            new EmojiInfo { Emoji = "🐂", Name = "Ox", Category = "Animals" },
            new EmojiInfo { Emoji = "🐃", Name = "Water Buffalo", Category = "Animals" },
            new EmojiInfo { Emoji = "🐄", Name = "Cow", Category = "Animals" },
            new EmojiInfo { Emoji = "🐮", Name = "Cow Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐷", Name = "Pig Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐖", Name = "Pig", Category = "Animals" },
            new EmojiInfo { Emoji = "🐏", Name = "Ram", Category = "Animals" },
            new EmojiInfo { Emoji = "🐑", Name = "Ewe", Category = "Animals" },
            new EmojiInfo { Emoji = "🐐", Name = "Goat", Category = "Animals" },
            new EmojiInfo { Emoji = "🐪", Name = "Camel", Category = "Animals" },
            new EmojiInfo { Emoji = "🐫", Name = "Two-Hump Camel", Category = "Animals" },
            new EmojiInfo { Emoji = "🦒", Name = "Giraffe", Category = "Animals" },
            new EmojiInfo { Emoji = "🐘", Name = "Elephant", Category = "Animals" },
            new EmojiInfo { Emoji = "🦣", Name = "Mammoth", Category = "Animals" },
            new EmojiInfo { Emoji = "🦏", Name = "Rhinoceros", Category = "Animals" },
            new EmojiInfo { Emoji = "🦛", Name = "Hippopotamus", Category = "Animals" },
            new EmojiInfo { Emoji = "🐆", Name = "Leopard", Category = "Animals" },
            new EmojiInfo { Emoji = "🐅", Name = "Tiger", Category = "Animals" },
            new EmojiInfo { Emoji = "🐯", Name = "Tiger Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🦁", Name = "Lion", Category = "Animals" },
            new EmojiInfo { Emoji = "🐎", Name = "Horse", Category = "Animals" },
            new EmojiInfo { Emoji = "🐴", Name = "Horse Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🦄", Name = "Unicorn", Category = "Animals" },
            new EmojiInfo { Emoji = "🦓", Name = "Zebra", Category = "Animals" },
            new EmojiInfo { Emoji = "🐒", Name = "Monkey", Category = "Animals" },
            new EmojiInfo { Emoji = "🦧", Name = "Gorilla", Category = "Animals" },
            new EmojiInfo { Emoji = "🦧", Name = "Orangutan", Category = "Animals" },
            new EmojiInfo { Emoji = "🐕", Name = "Dog", Category = "Animals" },
            new EmojiInfo { Emoji = "🐩", Name = "Poodle", Category = "Animals" },
            new EmojiInfo { Emoji = "🦮", Name = "Guide Dog", Category = "Animals" },
            new EmojiInfo { Emoji = "🐕‍🦺", Name = "Service Dog", Category = "Animals" },
            new EmojiInfo { Emoji = "🐈", Name = "Cat", Category = "Animals" },
            new EmojiInfo { Emoji = "🐈‍⬛", Name = "Black Cat", Category = "Animals" },
            new EmojiInfo { Emoji = "🐓", Name = "Rooster", Category = "Animals" },
            new EmojiInfo { Emoji = "🐔", Name = "Chicken", Category = "Animals" },
            new EmojiInfo { Emoji = "🦃", Name = "Turkey", Category = "Animals" },
            new EmojiInfo { Emoji = "🦚", Name = "Peacock", Category = "Animals" },
            new EmojiInfo { Emoji = "🦜", Name = "Parrot", Category = "Animals" },
            new EmojiInfo { Emoji = "🦢", Name = "Swan", Category = "Animals" },
            new EmojiInfo { Emoji = "🦩", Name = "Flamingo", Category = "Animals" },
            new EmojiInfo { Emoji = "🦦", Name = "Otter", Category = "Animals" },
            new EmojiInfo { Emoji = "🦨", Name = "Skunk", Category = "Animals" },
            new EmojiInfo { Emoji = "🦡", Name = "Badger", Category = "Animals" },
            new EmojiInfo { Emoji = "🐾", Name = "Paw Prints", Category = "Animals" },
            new EmojiInfo { Emoji = "🦴", Name = "Bone", Category = "Animals" },
            new EmojiInfo { Emoji = "🦵", Name = "Leg", Category = "Animals" },
            new EmojiInfo { Emoji = "🦶", Name = "Foot", Category = "Animals" },
            new EmojiInfo { Emoji = "🐵", Name = "Monkey Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐶", Name = "Dog Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐺", Name = "Wolf", Category = "Animals" },
            new EmojiInfo { Emoji = "🦊", Name = "Fox", Category = "Animals" },
            new EmojiInfo { Emoji = "🦝", Name = "Raccoon", Category = "Animals" },
            new EmojiInfo { Emoji = "🐱", Name = "Cat Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐲", Name = "Dragon Face", Category = "Animals" },
            new EmojiInfo { Emoji = "🐢", Name = "Turtle", Category = "Animals" },
            new EmojiInfo { Emoji = "🐸", Name = "Frog", Category = "Animals" },
            new EmojiInfo { Emoji = "🐍", Name = "Snake", Category = "Animals" },
            new EmojiInfo { Emoji = "🦎", Name = "Lizard", Category = "Animals" },
            new EmojiInfo { Emoji = "🦖", Name = "T-Rex", Category = "Animals" },
            new EmojiInfo { Emoji = "🦕", Name = "Sauropod", Category = "Animals" },
            new EmojiInfo { Emoji = "🐊", Name = "Crocodile", Category = "Animals" },
            new EmojiInfo { Emoji = "🐋", Name = "Whale", Category = "Animals" },
            new EmojiInfo { Emoji = "🐬", Name = "Dolphin", Category = "Animals" },
            new EmojiInfo { Emoji = "🐟", Name = "Fish", Category = "Animals" },
            new EmojiInfo { Emoji = "🐠", Name = "Tropical Fish", Category = "Animals" },
            new EmojiInfo { Emoji = "🐡", Name = "Blowfish", Category = "Animals" },
            new EmojiInfo { Emoji = "🦈", Name = "Shark", Category = "Animals" },
            new EmojiInfo { Emoji = "🐙", Name = "Octopus", Category = "Animals" },
            new EmojiInfo { Emoji = "🐚", Name = "Spiral Shell", Category = "Animals" },
            new EmojiInfo { Emoji = "🐌", Name = "Snail", Category = "Animals" },
            new EmojiInfo { Emoji = "🦋", Name = "Butterfly", Category = "Animals" },
            new EmojiInfo { Emoji = "🐛", Name = "Bug", Category = "Animals" },
            new EmojiInfo { Emoji = "🐜", Name = "Ant", Category = "Animals" },
            new EmojiInfo { Emoji = "🐝", Name = "Honeybee", Category = "Animals" },
            new EmojiInfo { Emoji = "🐞", Name = "Lady Beetle", Category = "Animals" },
            new EmojiInfo { Emoji = "🦗", Name = "Cricket", Category = "Animals" },
            new EmojiInfo { Emoji = "🕷️", Name = "Spider", Category = "Animals" },
            new EmojiInfo { Emoji = "🦂", Name = "Scorpion", Category = "Animals" },
            new EmojiInfo { Emoji = "🦟", Name = "Mosquito", Category = "Animals" },
            new EmojiInfo { Emoji = "🦠", Name = "Microbe", Category = "Animals" },
            new EmojiInfo { Emoji = "💐", Name = "Bouquet", Category = "Nature" },
            new EmojiInfo { Emoji = "🌸", Name = "Cherry Blossom", Category = "Nature" },
            new EmojiInfo { Emoji = "💮", Name = "White Flower", Category = "Nature" },
            new EmojiInfo { Emoji = "🏵️", Name = "Rosette", Category = "Nature" },
            new EmojiInfo { Emoji = "🌹", Name = "Rose", Category = "Nature" },
            new EmojiInfo { Emoji = "🥀", Name = "Wilted Flower", Category = "Nature" },
            new EmojiInfo { Emoji = "🌺", Name = "Hibiscus", Category = "Nature" },
            new EmojiInfo { Emoji = "🌻", Name = "Sunflower", Category = "Nature" },
            new EmojiInfo { Emoji = "🌼", Name = "Blossom", Category = "Nature" },
            new EmojiInfo { Emoji = "🌷", Name = "Tulip", Category = "Nature" },
            new EmojiInfo { Emoji = "🌱", Name = "Seedling", Category = "Nature" },
            new EmojiInfo { Emoji = "🌲", Name = "Evergreen Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌳", Name = "Deciduous Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌴", Name = "Palm Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌵", Name = "Cactus", Category = "Nature" },
            new EmojiInfo { Emoji = "🌾", Name = "Sheaf of Rice", Category = "Nature" },
            new EmojiInfo { Emoji = "🌿", Name = "Herb", Category = "Nature" },
            new EmojiInfo { Emoji = "☘️", Name = "Shamrock", Category = "Nature" },
            new EmojiInfo { Emoji = "🍀", Name = "Four Leaf Clover", Category = "Nature" },
            new EmojiInfo { Emoji = "🍁", Name = "Maple Leaf", Category = "Nature" },
            new EmojiInfo { Emoji = "🍂", Name = "Fallen Leaf", Category = "Nature" },
            new EmojiInfo { Emoji = "🍃", Name = "Leaf Fluttering in Wind", Category = "Nature" },
            new EmojiInfo { Emoji = "🍄", Name = "Mushroom", Category = "Nature" },
            new EmojiInfo { Emoji = "🌰", Name = "Chestnut", Category = "Nature" },
            new EmojiInfo { Emoji = "🦀", Name = "Crab", Category = "Animals" },
            new EmojiInfo { Emoji = "🦞", Name = "Lobster", Category = "Animals" },
            new EmojiInfo { Emoji = "🦐", Name = "Shrimp", Category = "Animals" },
            new EmojiInfo { Emoji = "🦑", Name = "Squid", Category = "Animals" },
            new EmojiInfo { Emoji = "🐙", Name = "Octopus", Category = "Animals" },
            new EmojiInfo { Emoji = "🦋", Name = "Butterfly", Category = "Animals" },
            new EmojiInfo { Emoji = "🐌", Name = "Snail", Category = "Animals" },
            new EmojiInfo { Emoji = "🐛", Name = "Bug", Category = "Animals" },
            new EmojiInfo { Emoji = "🐜", Name = "Ant", Category = "Animals" },
            new EmojiInfo { Emoji = "🐝", Name = "Honeybee", Category = "Animals" },
            new EmojiInfo { Emoji = "🐞", Name = "Lady Beetle", Category = "Animals" },
            new EmojiInfo { Emoji = "🦗", Name = "Cricket", Category = "Animals" },
            new EmojiInfo { Emoji = "🕷️", Name = "Spider", Category = "Animals" },
            new EmojiInfo { Emoji = "🦂", Name = "Scorpion", Category = "Animals" },
            new EmojiInfo { Emoji = "🦟", Name = "Mosquito", Category = "Animals" },
            new EmojiInfo { Emoji = "🦠", Name = "Microbe", Category = "Animals" },
            new EmojiInfo { Emoji = "💐", Name = "Bouquet", Category = "Nature" },
            new EmojiInfo { Emoji = "🌸", Name = "Cherry Blossom", Category = "Nature" },
            new EmojiInfo { Emoji = "💮", Name = "White Flower", Category = "Nature" },
            new EmojiInfo { Emoji = "🏵️", Name = "Rosette", Category = "Nature" },
            new EmojiInfo { Emoji = "🌹", Name = "Rose", Category = "Nature" },
            new EmojiInfo { Emoji = "🥀", Name = "Wilted Flower", Category = "Nature" },
            new EmojiInfo { Emoji = "🌺", Name = "Hibiscus", Category = "Nature" },
            new EmojiInfo { Emoji = "🌻", Name = "Sunflower", Category = "Nature" },
            new EmojiInfo { Emoji = "🌼", Name = "Blossom", Category = "Nature" },
            new EmojiInfo { Emoji = "🌷", Name = "Tulip", Category = "Nature" },
            new EmojiInfo { Emoji = "🌱", Name = "Seedling", Category = "Nature" },
            new EmojiInfo { Emoji = "🌲", Name = "Evergreen Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌳", Name = "Deciduous Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌴", Name = "Palm Tree", Category = "Nature" },
            new EmojiInfo { Emoji = "🌵", Name = "Cactus", Category = "Nature" },
            new EmojiInfo { Emoji = "🌾", Name = "Sheaf of Rice", Category = "Nature" },
            new EmojiInfo { Emoji = "🌿", Name = "Herb", Category = "Nature" },
            new EmojiInfo { Emoji = "☘️", Name = "Shamrock", Category = "Nature" },
            new EmojiInfo { Emoji = "🍀", Name = "Four Leaf Clover", Category = "Nature" },
            new EmojiInfo { Emoji = "🍁", Name = "Maple Leaf", Category = "Nature" },
            new EmojiInfo { Emoji = "🍂", Name = "Fallen Leaf", Category = "Nature" },
            new EmojiInfo { Emoji = "🍃", Name = "Leaf Fluttering in Wind", Category = "Nature" },
            new EmojiInfo { Emoji = "🍄", Name = "Mushroom", Category = "Nature" },
            new EmojiInfo { Emoji = "🌰", Name = "Chestnut", Category = "Nature" }
        };
    }

    public override void _Process(double delta)
    {
        if (_itemSearchPopup.Visible && _itemSearchList.Visible)
        {
            CheckHoverTip();
        }
    }

    private void CheckHoverTip()
    {
        var mousePos = GetViewport().GetMousePosition();
        var listRect = _itemSearchList.GetGlobalRect();
        
        if (!listRect.HasPoint(mousePos))
        {
            if (_currentHoverTip != null)
            {
                NHoverTipSet.Remove(_itemSearchList);
                _currentHoverTip = null;
                _lastHoveredIndex = -1;
            }
            return;
        }

        var localPos = mousePos - _itemSearchList.GlobalPosition;
        var hoveredIndex = GetItemAtPosition(localPos);
        
        if (hoveredIndex != _lastHoveredIndex)
        {
            _lastHoveredIndex = hoveredIndex;
            
            if (_currentHoverTip != null)
            {
                NHoverTipSet.Remove(_itemSearchList);
                _currentHoverTip = null;
            }
            
            if (hoveredIndex >= 0 && hoveredIndex < _searchResults.Count)
            {
                var item = _searchResults[hoveredIndex];
                ShowHoverTipForItem(item);
            }
        }
    }

    private int GetItemAtPosition(Vector2 localPos)
    {
        var itemHeight = _itemSearchList.GetThemeConstant("v_separation") + 24;
        var index = (int)(localPos.Y / itemHeight);
        
        if (index >= 0 && index < _itemSearchList.ItemCount)
        {
            return index;
        }
        return -1;
    }

    private void ShowHoverTipForItem(ItemSearchResult item)
    {
        try
        {
            if (item.Type == ItemReferenceSystem.ItemType.Card && item.CardModel != null)
            {
                var hoverTip = HoverTipFactory.FromCard(item.CardModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_itemSearchList, hoverTip, HoverTipAlignment.Right);
            }
            else if (item.Type == ItemReferenceSystem.ItemType.Potion && item.PotionModel != null)
            {
                var hoverTip = HoverTipFactory.FromPotion(item.PotionModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_itemSearchList, hoverTip, HoverTipAlignment.Right);
            }
            else if (item.Type == ItemReferenceSystem.ItemType.Relic && item.RelicModel != null)
            {
                var hoverTips = HoverTipFactory.FromRelic(item.RelicModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_itemSearchList, hoverTips, HoverTipAlignment.Right);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to show hover tip: {ex.Message}");
        }
    }

    private void OnColorPressed()
    {
        var rect = _colorBtn.GetGlobalRect();
        _colorPopup.Position = new Vector2I((int)rect.Position.X, (int)rect.End.Y + 4);
        _colorPopup.Popup();
    }

    private void OnColorSelected(long id)
    {
        var colors = new[] { "red", "green", "blue", "yellow", "cyan", "magenta", "orange", "purple", "gold", "white" };
        if (id >= 0 && id < colors.Length)
        {
            var color = colors[id];
            EmitSignal(SignalName.TagInserted, $"[color={color}][/color]");
        }
    }

    private void OnItemSearchRequested(BBcodeTagHelper.TagType type)
    {
        _currentSearchType = type;
        
        var title = type switch
        {
            BBcodeTagHelper.TagType.Card => LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_CARDS"),
            BBcodeTagHelper.TagType.Potion => LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_POTIONS"),
            BBcodeTagHelper.TagType.Relic => LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_RELICS"),
            _ => LocalizationManager.Instance.GetUI("CHATQAQ-SEARCH_ITEMS")
        };
        _itemSearchTitle.Text = title;

        _itemSearchInput.Text = "";
        _itemSearchList.Clear();
        _searchResults.Clear();
        _lastHoveredIndex = -1;

        LoadInitialItems(type);

        var rect = GetGlobalRect();
        var popupSize = new Vector2(300, 400);
        var popupPos = new Vector2I(
            (int)(rect.Position.X + rect.Size.X / 2 - popupSize.X / 2),
            (int)(rect.Position.Y - popupSize.Y - 4)
        );
        _itemSearchPopup.Position = popupPos;
        _itemSearchPopup.Popup();

        _itemSearchInput.GrabFocus();
    }

    private void LoadInitialItems(BBcodeTagHelper.TagType type)
    {
        _searchResults.Clear();
        _itemSearchList.Clear();

        var count = 0;
        const int maxItems = 50;

        if (type == BBcodeTagHelper.TagType.Card)
        {
            foreach (var card in ModelDb.AllCards)
            {
                if (count >= maxItems) break;
                _searchResults.Add(new ItemSearchResult
                {
                    Id = card.Id.ToString(),
                    Name = card.Title,
                    Type = ItemReferenceSystem.ItemType.Card,
                    CardModel = card
                });
                _itemSearchList.AddItem($"{card.Title} ({card.Rarity})");
                count++;
            }
        }
        else if (type == BBcodeTagHelper.TagType.Potion)
        {
            foreach (var potion in ModelDb.AllPotions)
            {
                if (count >= maxItems) break;
                var potionName = potion.Title.GetFormattedText();
                _searchResults.Add(new ItemSearchResult
                {
                    Id = potion.Id.ToString(),
                    Name = potionName,
                    Type = ItemReferenceSystem.ItemType.Potion,
                    PotionModel = potion
                });
                _itemSearchList.AddItem($"{potionName} ({potion.Rarity})");
                count++;
            }
        }
        else if (type == BBcodeTagHelper.TagType.Relic)
        {
            foreach (var relic in ModelDb.AllRelics)
            {
                if (count >= maxItems) break;
                var relicName = relic.Title.GetFormattedText();
                _searchResults.Add(new ItemSearchResult
                {
                    Id = relic.Id.ToString(),
                    Name = relicName,
                    Type = ItemReferenceSystem.ItemType.Relic,
                    RelicModel = relic
                });
                _itemSearchList.AddItem($"{relicName} ({relic.Rarity})");
                count++;
            }
        }
    }

    private void OnSearchTextChanged(string text)
    {
        _searchResults.Clear();
        _itemSearchList.Clear();
        _lastHoveredIndex = -1;

        if (string.IsNullOrWhiteSpace(text))
        {
            LoadInitialItems(_currentSearchType);
            return;
        }

        var count = 0;
        const int maxItems = 50;

        if (_currentSearchType == BBcodeTagHelper.TagType.Card)
        {
            foreach (var card in ModelDb.AllCards)
            {
                if (count >= maxItems) break;
                if (card.Title.ToLower().Contains(text.ToLower()) ||
                    card.Id.Entry.ToLower().Contains(text.ToLower()))
                {
                    _searchResults.Add(new ItemSearchResult
                    {
                        Id = card.Id.ToString(),
                        Name = card.Title,
                        Type = ItemReferenceSystem.ItemType.Card,
                        CardModel = card
                    });
                    _itemSearchList.AddItem($"{card.Title} ({card.Rarity})");
                    count++;
                }
            }
        }
        else if (_currentSearchType == BBcodeTagHelper.TagType.Potion)
        {
            foreach (var potion in ModelDb.AllPotions)
            {
                if (count >= maxItems) break;
                var potionName = potion.Title.GetFormattedText();
                if (potionName.ToLower().Contains(text.ToLower()) ||
                    potion.Id.Entry.ToLower().Contains(text.ToLower()))
                {
                    _searchResults.Add(new ItemSearchResult
                    {
                        Id = potion.Id.ToString(),
                        Name = potionName,
                        Type = ItemReferenceSystem.ItemType.Potion,
                        PotionModel = potion
                    });
                    _itemSearchList.AddItem($"{potionName} ({potion.Rarity})");
                    count++;
                }
            }
        }
        else if (_currentSearchType == BBcodeTagHelper.TagType.Relic)
        {
            foreach (var relic in ModelDb.AllRelics)
            {
                if (count >= maxItems) break;
                var relicName = relic.Title.GetFormattedText();
                if (relicName.ToLower().Contains(text.ToLower()) ||
                    relic.Id.Entry.ToLower().Contains(text.ToLower()))
                {
                    _searchResults.Add(new ItemSearchResult
                    {
                        Id = relic.Id.ToString(),
                        Name = relicName,
                        Type = ItemReferenceSystem.ItemType.Relic,
                        RelicModel = relic
                    });
                    _itemSearchList.AddItem($"{relicName} ({relic.Rarity})");
                    count++;
                }
            }
        }
    }

    private void OnItemSelected(long index)
    {
        if (_currentHoverTip != null)
        {
            NHoverTipSet.Remove(_itemSearchList);
            _currentHoverTip = null;
        }

        if (index < 0 || index >= _searchResults.Count)
        {
            return;
        }

        var item = _searchResults[(int)index];
        var tagHelper = BBcodeTagHelper.Instance;

        string tag = item.Type switch
        {
            ItemReferenceSystem.ItemType.Card => tagHelper.CreateCardTag(item.Id, item.Name),
            ItemReferenceSystem.ItemType.Potion => tagHelper.CreatePotionTag(item.Id, item.Name),
            ItemReferenceSystem.ItemType.Relic => tagHelper.CreateRelicTag(item.Id, item.Name),
            _ => ""
        };

        if (!string.IsNullOrEmpty(tag))
        {
            EmitSignal(SignalName.TagInserted, tag);
        }

        _itemSearchPopup.Hide();
    }

    private void OnEmojiBtnPressed()
    {
        if (_emojiPopup == null)
        {
            SetupEmojiPopup();
        }

        var rect = _emojiBtn.GetGlobalRect();
        _emojiPopup!.Position = new Vector2I((int)rect.Position.X, (int)rect.End.Y + 4);
        _emojiPopup!.Popup();
    }

    private void OnEmojiBtnSelected(string emoji)
    {
        EmitSignal(SignalName.TagInserted, emoji);
        _emojiPopup?.Hide();
    }

    public void ShowPopup()
    {
        Show();
    }

    public void HidePopup()
    {
        Hide();
        _itemSearchPopup.Hide();
        _colorPopup.Hide();
        
        if (_currentHoverTip != null)
        {
            NHoverTipSet.Remove(_itemSearchList);
            _currentHoverTip = null;
        }
    }
}
