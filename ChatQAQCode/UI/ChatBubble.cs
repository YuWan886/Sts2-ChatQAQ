using Godot;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class ChatBubble : Control
{
    private Panel _backgroundPanel = null!;
    private Panel _tailPanel = null!;
    private RichTextLabel _messageLabel = null!;
    private Godot.Timer _displayTimer = null!;
    private Tween _currentTween = null!;

    public Queue<string> MessageQueue { get; private set; } = new Queue<string>();
    public bool IsShowing { get; private set; } = false;
    public float CurrentDuration { get; private set; } = 3.0f;
    public float FadeDuration { get; set; } = 0.3f;

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(180, 55);
        SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;

        _backgroundPanel = new Panel();
        _backgroundPanel.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundPanel.CustomMinimumSize = new Vector2(180, 55);
        AddChild(_backgroundPanel);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0.1f, 0.08f, 0.06f, 0.92f);
        styleBox.BorderColor = new Color("5A4A3A");
        styleBox.SetBorderWidthAll(2);
        styleBox.SetCornerRadiusAll(10);
        styleBox.SetContentMarginAll(10);
        styleBox.CornerDetail = 8;
        styleBox.AntiAliasing = true;
        styleBox.ShadowColor = new Color(0, 0, 0, 0.3f);
        styleBox.ShadowSize = 4;
        styleBox.ShadowOffset = new Vector2(2, 2);
        _backgroundPanel.AddThemeStyleboxOverride("panel", styleBox);

        _tailPanel = new Panel();
        _tailPanel.CustomMinimumSize = new Vector2(14, 10);
        _tailPanel.Position = new Vector2(18, _backgroundPanel.CustomMinimumSize.Y - 2);
        _tailPanel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_tailPanel);

        var tailStyle = new StyleBoxFlat();
        tailStyle.BgColor = new Color(0.1f, 0.08f, 0.06f, 0.92f);
        tailStyle.SetCornerRadiusAll(2);
        _tailPanel.AddThemeStyleboxOverride("panel", tailStyle);
        _tailPanel.Rotation = Mathf.DegToRad(45);

        var contentContainer = new MarginContainer();
        contentContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        contentContainer.AddThemeConstantOverride("margin_left", 10);
        contentContainer.AddThemeConstantOverride("margin_right", 10);
        contentContainer.AddThemeConstantOverride("margin_top", 8);
        contentContainer.AddThemeConstantOverride("margin_bottom", 8);
        _backgroundPanel.AddChild(contentContainer);

        _messageLabel = new RichTextLabel();
        _messageLabel.BbcodeEnabled = true;
        _messageLabel.FitContent = true;
        _messageLabel.ScrollActive = false;
        _messageLabel.CustomMinimumSize = new Vector2(160, 28);
        _messageLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _messageLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
        _messageLabel.AddThemeColorOverride("default_color", StsUiStyles.Cream);
        _messageLabel.AddThemeFontSizeOverride("normal_font_size", 13);
        _messageLabel.MouseFilter = MouseFilterEnum.Ignore;
        contentContainer.AddChild(_messageLabel);

        _displayTimer = new Godot.Timer();
        _displayTimer.OneShot = true;
        _displayTimer.Timeout += OnDisplayTimerTimeout;
        AddChild(_displayTimer);

        Modulate = new Color(1, 1, 1, 0);
        Scale = new Vector2(0.7f, 0.7f);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (!IsShowing && MessageQueue.Count > 0)
        {
            var nextMessage = MessageQueue.Dequeue();
            ShowMessage(nextMessage, CurrentDuration);
        }
    }

    public void ShowMessage(string content, float duration)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        if (IsShowing)
        {
            MessageQueue.Enqueue(content);
            return;
        }

        IsShowing = true;
        CurrentDuration = duration;
        Visible = true;

        _messageLabel.Text = content;

        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.SetEase(Tween.EaseType.Out);
        _currentTween.SetTrans(Tween.TransitionType.Back);

        _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), 0.2);
        _currentTween.Parallel().TweenProperty(this, "scale", new Vector2(1, 1), 0.2);

        _displayTimer.WaitTime = duration;
        _displayTimer.Start();
    }

    public void HideMessage()
    {
        if (!IsShowing)
        {
            return;
        }

        _displayTimer.Stop();
        StartFadeOut();
    }

    private void StartFadeOut()
    {
        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.SetEase(Tween.EaseType.In);
        _currentTween.SetTrans(Tween.TransitionType.Quad);

        _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), FadeDuration);
        _currentTween.Parallel().TweenProperty(this, "scale", new Vector2(0.85f, 0.85f), FadeDuration);

        _currentTween.TweenCallback(Callable.From(OnFadeOutComplete));
    }

    private void OnFadeOutComplete()
    {
        IsShowing = false;
        Visible = false;
        Scale = new Vector2(0.7f, 0.7f);
        Modulate = new Color(1, 1, 1, 0);
    }

    private void OnDisplayTimerTimeout()
    {
        StartFadeOut();
    }

    public void QueueNextMessage(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        MessageQueue.Enqueue(content);
    }

    public void ClearQueue()
    {
        MessageQueue.Clear();
    }

    public void ForceHide()
    {
        _currentTween?.Kill();
        _displayTimer.Stop();
        OnFadeOutComplete();
    }
}
