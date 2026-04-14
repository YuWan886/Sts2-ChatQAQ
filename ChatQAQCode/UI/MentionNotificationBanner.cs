using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.TestSupport;
using ChatQAQ.ChatQAQCode.Core;
using MegaCrit.Sts2.addons.mega_text;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class MentionNotificationBanner : Control
{
    private RichTextLabel _label = null!;
    private RichTextLabel? _subLabel;
    private Tween? _tween;
    private string _mainText = "";
    private string _subText = "";

    public static MentionNotificationBanner? Create(string mainText, string subText = "")
    {
        if (TestMode.IsOn)
        {
            return null;
        }

        var banner = new MentionNotificationBanner();
        banner._mainText = mainText;
        banner._subText = subText;
        return banner;
    }

    public override void _Ready()
    {
        MainFile.Logger.Info($"MentionNotificationBanner._Ready called with mainText='{_mainText}', subText='{_subText}'");
        
        MouseFilter = MouseFilterEnum.Ignore;
        
        SetAnchorsPreset(LayoutPreset.Center);
        SetOffsetsPreset(LayoutPreset.Center);
        OffsetLeft = -400;
        OffsetTop = -60;
        OffsetRight = 400;
        OffsetBottom = 60;
        
        // 处理 BBCode 标签，转换自定义标签为标准 BBCode
        var processedMainText = BBcodeTagHelper.Instance.ConvertCustomTagsToBbcode(_mainText);
        processedMainText = BBcodeTagHelper.Instance.ValidateAndFixBbcode(processedMainText);
        processedMainText = BBcodeTagHelper.Instance.EscapeMentionNames(processedMainText);
        
        var processedSubText = !string.IsNullOrEmpty(_subText) 
            ? BBcodeTagHelper.Instance.ConvertCustomTagsToBbcode(_subText) 
            : "";
        processedSubText = BBcodeTagHelper.Instance.ValidateAndFixBbcode(processedSubText);
        processedSubText = BBcodeTagHelper.Instance.EscapeMentionNames(processedSubText);
        
        _label = new RichTextLabel();
        _label.Name = "Label";
        _label.SetAnchorsPreset(LayoutPreset.Center);
        _label.OffsetLeft = -400;
        _label.OffsetTop = -30;
        _label.OffsetRight = 400;
        _label.OffsetBottom = 30;
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f, 1f));
        _label.AddThemeFontSizeOverride("normal_font_size", 50);
        _label.BbcodeEnabled = true;
        _label.Text = processedMainText;
        AddChild(_label);

        if (!string.IsNullOrEmpty(processedSubText))
        {
            _subLabel = new RichTextLabel();
            _subLabel.Name = "SubLabel";
            _subLabel.SetAnchorsPreset(LayoutPreset.Center);
            _subLabel.OffsetLeft = -400;
            _subLabel.OffsetTop = 30;
            _subLabel.OffsetRight = 400;
            _subLabel.OffsetBottom = 90;
            _subLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _subLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.9f, 0.8f, 1f));
            _subLabel.AddThemeFontSizeOverride("normal_font_size", 32);
            _subLabel.BbcodeEnabled = true;
            var truncatedSubText = processedSubText.Length > 60 ? processedSubText.Substring(0, 57) + "..." : processedSubText;
            _subLabel.Text = truncatedSubText;
            AddChild(_subLabel);
        }

        Modulate = Colors.Transparent;
        
        MainFile.Logger.Info($"MentionNotificationBanner._Ready completed, Modulate={Modulate}");

        TaskHelper.RunSafely(Display());
    }

    private async Task Display()
    {
        MainFile.Logger.Info($"MentionNotificationBanner.Display started");
        
        PlayNotificationSound();

        _tween = CreateTween();
        _tween.SetParallel();
        _tween.TweenProperty(this, "modulate:a", 1f, 1.0).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        _tween.TweenProperty(_label, "position", _label.Position + new Vector2(0f, -50f), 1.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        if (_subLabel != null)
        {
            _tween.Parallel().TweenProperty(_subLabel, "position", _subLabel.Position + new Vector2(0f, 50f), 1.5).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
        }
        await ToSignal(_tween, Tween.SignalName.Finished);
        
        MainFile.Logger.Info($"MentionNotificationBanner: Fade in completed");

        _tween = CreateTween();
        _tween.TweenInterval(3.0);
        _tween.TweenProperty(this, "modulate:a", 0f, 0.3).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        await ToSignal(_tween, Tween.SignalName.Finished);
        
        MainFile.Logger.Info($"MentionNotificationBanner: Fade out completed, calling QueueFreeSafely");
        this.QueueFreeSafely();
    }

    private void PlayNotificationSound()
    {
        try
        {
            NDebugAudioManager.Instance?.Play("map_ping.mp3", 1f, PitchVariance.Medium);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to play notification sound: {ex.Message}");
        }
    }

    public override void _ExitTree()
    {
        _tween?.Kill();
    }
}
