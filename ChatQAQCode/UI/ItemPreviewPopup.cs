using Godot;
using ChatQAQ.ChatQAQCode.Core;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using static Godot.Control;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class ItemPreviewPopup : PopupPanel
{
    private VBoxContainer _mainContainer = null!;
    private HBoxContainer _headerContainer = null!;
    private Label _titleLabel = null!;
    private Label _rarityLabel = null!;
    private RichTextLabel _descriptionLabel = null!;
    private TextureRect _iconRect = null!;
    private Control _iconPlaceholder = null!;

    private readonly Vector2 _popupSize = new Vector2(280, 180);

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.06f, 0.04f, 0.98f);
        style.SetBorderWidthAll(2);
        style.BorderColor = StsUiStyles.PanelBorder;
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(12);
        style.ShadowColor = new Color(0, 0, 0, 0.4f);
        style.ShadowSize = 6;
        style.ShadowOffset = new Vector2(2, 2);
        AddThemeStyleboxOverride("panel", style);

        _mainContainer = new VBoxContainer();
        _mainContainer.CustomMinimumSize = _popupSize;
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 8);
        AddChild(_mainContainer);

        _headerContainer = new HBoxContainer();
        _headerContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _headerContainer.AddThemeConstantOverride("separation", 10);
        _mainContainer.AddChild(_headerContainer);

        _iconPlaceholder = new Control();
        _iconPlaceholder.CustomMinimumSize = new Vector2(48, 48);
        _headerContainer.AddChild(_iconPlaceholder);

        _iconRect = new TextureRect();
        _iconRect.CustomMinimumSize = new Vector2(48, 48);
        _iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        _iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _iconRect.Visible = false;
        _iconPlaceholder.AddChild(_iconRect);

        var titleVBox = new VBoxContainer();
        titleVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleVBox.AddThemeConstantOverride("separation", 2);
        _headerContainer.AddChild(titleVBox);

        _titleLabel = new Label();
        _titleLabel.Text = "";
        _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _titleLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _titleLabel.AddThemeColorOverride("font_color", StsUiStyles.Gold);
        _titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleVBox.AddChild(_titleLabel);

        _rarityLabel = new Label();
        _rarityLabel.Text = "";
        _rarityLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rarityLabel.AddThemeColorOverride("font_color", StsUiStyles.TextMuted);
        _rarityLabel.AddThemeFontSizeOverride("font_size", 12);
        titleVBox.AddChild(_rarityLabel);

        var separator = new HSeparator();
        separator.CustomMinimumSize = new Vector2(0, 2);
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = StsUiStyles.PanelBorder;
        separator.AddThemeStyleboxOverride("separator", sepStyle);
        _mainContainer.AddChild(separator);

        _descriptionLabel = new RichTextLabel();
        _descriptionLabel.BbcodeEnabled = true;
        _descriptionLabel.FitContent = true;
        _descriptionLabel.ScrollActive = false;
        _descriptionLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _descriptionLabel.AddThemeColorOverride("default_color", StsUiStyles.TextPrimary);
        _descriptionLabel.AddThemeFontSizeOverride("normal_font_size", 13);
        _mainContainer.AddChild(_descriptionLabel);
    }

    public void ShowCard(CardModel card)
    {
        if (card == null)
        {
            Hide();
            return;
        }

        _titleLabel.Text = card.Title;
        _rarityLabel.Text = $"{LocalizationManager.Instance.GetUI("CHATQAQ-TYPE_CARD")} - {card.Rarity}";
        
        var hoverTip = HoverTipFactory.FromCard(card);
        if (hoverTip is CardHoverTip cardTip)
        {
            var mutableCard = cardTip.Card;
            _descriptionLabel.Text = mutableCard.Description?.GetFormattedText() ?? "";
        }
        else
        {
            _descriptionLabel.Text = "";
        }

        ShowIcon(card.PortraitPath);
        Popup();
    }

    public void ShowPotion(PotionModel potion)
    {
        if (potion == null)
        {
            Hide();
            return;
        }

        _titleLabel.Text = potion.Title.GetFormattedText();
        _rarityLabel.Text = $"{LocalizationManager.Instance.GetUI("CHATQAQ-TYPE_POTION")} - {potion.Rarity}";
        
        var hoverTip = HoverTipFactory.FromPotion(potion);
        if (hoverTip is HoverTip tip)
        {
            _descriptionLabel.Text = tip.Description ?? "";
        }
        else
        {
            _descriptionLabel.Text = "";
        }

        ShowIcon(potion.ImagePath);
        Popup();
    }

    public void ShowRelic(RelicModel relic)
    {
        if (relic == null)
        {
            Hide();
            return;
        }

        _titleLabel.Text = relic.Title.GetFormattedText();
        _rarityLabel.Text = $"{LocalizationManager.Instance.GetUI("CHATQAQ-TYPE_RELIC")} - {relic.Rarity}";
        
        var hoverTips = HoverTipFactory.FromRelic(relic);
        if (hoverTips != null)
        {
            foreach (var t in hoverTips)
            {
                if (t is HoverTip tip)
                {
                    _descriptionLabel.Text = tip.Description ?? "";
                    break;
                }
            }
        }
        else
        {
            _descriptionLabel.Text = "";
        }

        ShowIcon(relic.IconPath);
        Popup();
    }

    private void ShowIcon(string iconPath)
    {
        if (!string.IsNullOrEmpty(iconPath))
        {
            var texture = GD.Load<Texture2D>(iconPath);
            if (texture != null)
            {
                _iconRect.Texture = texture;
                _iconRect.Visible = true;
            }
            else
            {
                _iconRect.Visible = false;
            }
        }
        else
        {
            _iconRect.Visible = false;
        }
    }

    public void ShowAtPosition(Vector2 globalPosition)
    {
        var viewport = GetViewport();
        var viewportSize = viewport != null ? viewport.GetVisibleRect().Size : new Vector2(1920, 1080);
        var popupSize = _popupSize;

        var x = globalPosition.X + 10;
        var y = globalPosition.Y + 10;

        if (x + popupSize.X > viewportSize.X)
        {
            x = globalPosition.X - popupSize.X - 10;
        }

        if (y + popupSize.Y > viewportSize.Y)
        {
            y = globalPosition.Y - popupSize.Y - 10;
        }

        Position = new Vector2I((int)x, (int)y);
    }
}
