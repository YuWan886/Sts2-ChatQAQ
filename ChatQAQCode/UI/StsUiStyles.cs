using Godot;

namespace ChatQAQ.ChatQAQCode.UI;

public static class StsUiStyles
{
    public static readonly Color Cream = new Color("FFF6E2");
    public static readonly Color Gold = new Color("EFC851");
    public static readonly Color Aqua = new Color("2AEBBE");
    public static readonly Color Red = new Color("FF5555");
    public static readonly Color Green = new Color("7FFF00");
    public static readonly Color Orange = new Color("FFA518");
    public static readonly Color Blue = new Color("87CEEB");

    public static readonly Color PanelBg = new Color(0.05f, 0.05f, 0.08f, 0.95f);
    public static readonly Color PanelBorder = new Color("3D3428");
    public static readonly Color PanelBorderHighlight = new Color("6B5A3E");

    public static readonly Color ButtonNormal = new Color("4A3F2F");
    public static readonly Color ButtonHover = new Color("6B5A3E");
    public static readonly Color ButtonPressed = new Color("3D3428");
    public static readonly Color ButtonDisabled = new Color(0.3f, 0.3f, 0.3f, 0.75f);

    public static readonly Color InputBg = new Color(0.08f, 0.08f, 0.12f, 0.9f);
    public static readonly Color InputBorder = new Color("4A3F2F");

    public static readonly Color TextPrimary = Cream;
    public static readonly Color TextSecondary = new Color(0.7f, 0.7f, 0.7f, 1.0f);
    public static readonly Color TextMuted = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    public static readonly Color LocalPlayerBg = new Color(0.08f, 0.12f, 0.18f, 0.9f);
    public static readonly Color LocalPlayerBorder = new Color("4A6B8A");
    public static readonly Color OtherPlayerBg = new Color(0.1f, 0.08f, 0.06f, 0.9f);
    public static readonly Color OtherPlayerBorder = new Color("6B5A3E");

    public static StyleBoxFlat CreatePanelStyle(float borderRadius = 8f, float borderWidth = 2f, float padding = 12f)
    {
        var style = new StyleBoxFlat();
        style.BgColor = PanelBg;
        style.BorderColor = PanelBorder;
        style.SetBorderWidthAll((int)borderWidth);
        style.SetCornerRadiusAll((int)borderRadius);
        style.SetContentMarginAll(padding);
        style.CornerDetail = 8;
        style.AntiAliasing = true;
        return style;
    }

    public static StyleBoxFlat CreateButtonStyle(bool isHovered = false, bool isPressed = false)
    {
        var style = new StyleBoxFlat();
        style.BgColor = isPressed ? ButtonPressed : (isHovered ? ButtonHover : ButtonNormal);
        style.BorderColor = PanelBorderHighlight;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(6);
        return style;
    }

    public static StyleBoxFlat CreateInputStyle()
    {
        var style = new StyleBoxFlat();
        style.BgColor = InputBg;
        style.BorderColor = InputBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(4);
        style.SetContentMarginAll(8);
        return style;
    }

    public static StyleBoxFlat CreateMessageStyle(bool isLocalPlayer)
    {
        var style = new StyleBoxFlat();
        style.BgColor = isLocalPlayer ? LocalPlayerBg : OtherPlayerBg;
        style.BorderColor = isLocalPlayer ? LocalPlayerBorder : OtherPlayerBorder;
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(8);
        return style;
    }

    public static void ApplyButtonStyles(Button button)
    {
        var normalStyle = CreateButtonStyle(false, false);
        var hoverStyle = CreateButtonStyle(true, false);
        var pressedStyle = CreateButtonStyle(false, true);

        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeColorOverride("font_color", TextPrimary);
        button.AddThemeColorOverride("font_hover_color", Gold);
        button.AddThemeColorOverride("font_pressed_color", Cream);
    }

    public static Button CreateCloseButton()
    {
        var btn = new Button();
        btn.Text = "X";
        btn.CustomMinimumSize = new Vector2(28, 28);

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = new Color(0.4f, 0.15f, 0.15f, 0.9f);
        normalStyle.SetCornerRadiusAll(4);
        normalStyle.SetContentMarginAll(4);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(0.6f, 0.2f, 0.2f, 1.0f);
        hoverStyle.SetCornerRadiusAll(4);
        hoverStyle.SetContentMarginAll(4);

        var pressedStyle = new StyleBoxFlat();
        pressedStyle.BgColor = new Color(0.3f, 0.1f, 0.1f, 1.0f);
        pressedStyle.SetCornerRadiusAll(4);
        pressedStyle.SetContentMarginAll(4);

        btn.AddThemeStyleboxOverride("normal", normalStyle);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);
        btn.AddThemeStyleboxOverride("pressed", pressedStyle);
        btn.AddThemeColorOverride("font_color", Cream);
        btn.AddThemeColorOverride("font_hover_color", new Color("FFFFFF"));
        btn.AddThemeFontSizeOverride("font_size", 14);

        return btn;
    }

    public static Button CreateStsButton(string text, Vector2? minSize = null)
    {
        var btn = new Button();
        btn.Text = text;
        if (minSize.HasValue)
        {
            btn.CustomMinimumSize = minSize.Value;
        }
        ApplyButtonStyles(btn);
        return btn;
    }

    public static Label CreateTitleLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", Gold);
        label.AddThemeFontSizeOverride("font_size", 20);
        return label;
    }

    public static Label CreateLabel(string text, Color? color = null)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", color ?? TextPrimary);
        label.AddThemeFontSizeOverride("font_size", 14);
        return label;
    }
}
