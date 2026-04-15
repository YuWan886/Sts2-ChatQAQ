using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class ChatBubbleWithHoverTips : Control
{
    private static readonly Regex ItemTagPattern = new Regex(
        @"\[(card|potion|relic)=([^\]]+)\]([^\[]+)\[/\1\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private Node? _bubbleScene;
    private Control? _container;
    private MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel? _mainLabel;
    private Tween? _tween;
    private double _displayDuration = 3.0;
    private string _rawText = "";
    private List<ItemTagInfo> _itemTags = new();
    private NHoverTipSet? _currentHoverTip;
    private int _currentHoveredTagIndex = -1;
    
    private Creature? _speaker;
    private bool _isReady = false;
    private bool _animationStarted = false;
    private DialogueSide _side = DialogueSide.Left;
    private bool _pendingSetup = false;
    private string? _pendingText;
    private double _pendingDuration = 3.0;
    
    private const float HoverTipScale = 0.6f;

    private class ItemTagInfo
    {
        public string Type { get; set; } = "";
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public CardModel? CardModel { get; set; }
        public PotionModel? PotionModel { get; set; }
        public RelicModel? RelicModel { get; set; }
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        FocusMode = FocusModeEnum.None;
        
        try
        {
            _bubbleScene = PreloadManager.Cache.GetScene("res://scenes/vfx/vfx_speech_bubble.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to load bubble scene: {ex.Message}");
        }
        
        if (_bubbleScene is Control bubbleControl)
        {
            AddChild(bubbleControl);
            
            _container = bubbleControl.GetNode<Control>("%Container");

            
            var megaLabel = bubbleControl.GetNode<MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel>("%Text");
            if (megaLabel != null && _container != null)
            {
                _container.CustomMinimumSize = new Vector2(180, 55);
                
                _mainLabel = megaLabel;
                _mainLabel.BbcodeEnabled = true;
                _mainLabel.ScrollActive = false;
                _mainLabel.MouseFilter = MouseFilterEnum.Pass;
                _mainLabel.AddThemeColorOverride("normal_font_color", new Color(0.9f, 0.85f, 0.75f));
                _mainLabel.AddThemeFontSizeOverride("normal_font_size", 13);
                
                _mainLabel.MetaHoverStarted += OnMetaHoverStarted;
                _mainLabel.MetaHoverEnded += OnMetaHoverEnded;
                

            }
            else
            {
                MainFile.Logger.Warn($"Failed to get %Text or %Container: megaLabel={megaLabel != null}, container={_container != null}");
            }
            
            _bubbleSprite = bubbleControl.GetNode<Sprite2D>("%Bubble");
            _shadowSprite = bubbleControl.GetNode<Sprite2D>("%Shadow");
        }
        else
        {
            CreateFallbackBubble();
        }
        
        _isReady = true;
        
        if (_pendingSetup && _pendingText != null)
        {
            DoSetup(_pendingText, _speaker!, _pendingDuration);
            _pendingSetup = false;
            _pendingText = null;
        }
    }

    private Sprite2D? _bubbleSprite;
    private Sprite2D? _shadowSprite;

    private void CreateFallbackBubble()
    {
        var panel = new Panel();
        panel.MouseFilter = MouseFilterEnum.Stop;
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.08f, 0.06f, 0.95f);
        style.SetBorderWidthAll(2);
        style.BorderColor = new Color(0.4f, 0.35f, 0.25f);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(12);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        var fallbackLabel = new RichTextLabel();
        fallbackLabel.BbcodeEnabled = true;
        fallbackLabel.ScrollActive = false;
        fallbackLabel.MouseFilter = MouseFilterEnum.Stop;
        fallbackLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        fallbackLabel.AddThemeColorOverride("normal_font_color", new Color(0.9f, 0.85f, 0.75f));
        fallbackLabel.AddThemeFontSizeOverride("normal_font_size", 13);
        panel.AddChild(fallbackLabel);
        
        _mainLabel = (MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel)(object)fallbackLabel;
        
        _mainLabel.MetaHoverStarted += OnMetaHoverStarted;
        _mainLabel.MetaHoverEnded += OnMetaHoverEnded;
    }

    public void Setup(string text, Creature speaker, double duration)
    {
        if (!_isReady)
        {
            _pendingSetup = true;
            _pendingText = text;
            _pendingDuration = duration;
            _speaker = speaker;
            return;
        }

        DoSetup(text, speaker, duration);
    }
    
    private void DoSetup(string text, Creature speaker, double duration)
    {
        _rawText = text;
        _speaker = speaker;
        _displayDuration = duration;
        
        MainFile.Logger.Info($"ChatBubbleWithHoverTips.DoSetup: duration={duration}, _displayDuration={_displayDuration}");
        
        if (speaker != null)
        {
            _side = speaker.Side == CombatSide.Player ? DialogueSide.Left : DialogueSide.Right;
        }
        
        ParseItemTags(text);
        
        if (_container != null)
        {
            _container.CustomMinimumSize = new Vector2(180, 55);
        }

        SetupContent();
        StartAnimation();
    }

    private void SetupContent()
    {
        if (_mainLabel == null || string.IsNullOrEmpty(_rawText))
        {
            return;
        }

        ApplyBubbleOrientation();

        var displayText = BuildDisplayText(_rawText);
        _mainLabel.Text = displayText;
        _mainLabel.Visible = true;

        if (_speaker != null)
        {
            PositionBubble(_speaker);
        }
    }

    private void ApplyBubbleOrientation()
    {
        if (_side == DialogueSide.Right)
        {
            if (_container != null)
            {
                _container.Position = new Vector2(-_container.Size.X - (_container?.Position.X ?? 0), _container?.Position.Y ?? 0);
            }
            if (_bubbleSprite != null) _bubbleSprite.FlipH = true;
            if (_shadowSprite != null) _shadowSprite.FlipH = true;
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
        if (_mainLabel == null || index < 0 || index >= _itemTags.Count) return;
        
        ClearHoverTip();
        
        var tag = _itemTags[index];
        try
        {
            if (tag.Type == "card" && tag.CardModel != null)
            {
                var hoverTip = HoverTipFactory.FromCard(tag.CardModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_mainLabel, hoverTip, HoverTipAlignment.Center);
            }
            else if (tag.Type == "potion" && tag.PotionModel != null)
            {
                var hoverTip = HoverTipFactory.FromPotion(tag.PotionModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_mainLabel, hoverTip, HoverTipAlignment.Center);
            }
            else if (tag.Type == "relic" && tag.RelicModel != null)
            {
                var hoverTips = HoverTipFactory.FromRelic(tag.RelicModel);
                _currentHoverTip = NHoverTipSet.CreateAndShow(_mainLabel, hoverTips, HoverTipAlignment.Center);
            }
            
            if (_currentHoverTip != null)
            {
                _currentHoverTip.Scale = new Vector2(HoverTipScale, HoverTipScale);
                
                var mousePos = GetGlobalMousePosition();
                _currentHoverTip.GlobalPosition = new Vector2(mousePos.X + 10, mousePos.Y - _currentHoverTip.Size.Y - 10);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to show hover tip: {ex.Message}");
        }
    }

    private Color GetTagColor(string type)
    {
        return type switch
        {
            "card" => new Color(0.4f, 0.7f, 1.0f),
            "potion" => new Color(0.6f, 1.0f, 0.4f),
            "relic" => new Color(1.0f, 0.85f, 0.3f),
            _ => new Color(0.9f, 0.85f, 0.75f)
        };
    }

    private void PositionBubble(Creature speaker)
    {
        try
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;
            
            var creatureNode = combatRoom.GetCreatureNode(speaker);
            if (creatureNode == null) return;
            
            Vector2 spawnPos;
            if (creatureNode.Visuals.TalkPosition != null)
            {
                spawnPos = creatureNode.Visuals.TalkPosition.GlobalPosition;
            }
            else
            {
                spawnPos = creatureNode.VfxSpawnPosition + new Vector2(0f, -creatureNode.Hitbox.Size.Y * 0.5f * 0.75f);
                if (speaker.Side == CombatSide.Player)
                {
                    spawnPos.X += creatureNode.Hitbox.Size.X * 0.75f;
                }
                else
                {
                    spawnPos.X -= creatureNode.Hitbox.Size.X * 0.75f;
                }
            }
            
            GlobalPosition = spawnPos;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to position bubble: {ex.Message}");
        }
    }

    public void AnimateIn()
    {
        if (_isReady)
        {
            StartAnimation();
        }
    }

    private void StartAnimation()
    {
        if (_animationStarted) return;
        
        _animationStarted = true;
        Modulate = new Color(1, 1, 1, 0);
        Scale = new Vector2(0.5f, 0.5f);
        
        _tween = CreateTween();
        _tween.TweenProperty(this, "modulate:a", 1f, 0.3f);
        _tween.Parallel().TweenProperty(this, "scale", Vector2.One, 0.3f).SetEase(Tween.EaseType.Out);
        
        _tween.Finished += OnAnimateInFinished;
    }

    private void OnAnimateInFinished()
    {
        var displayTime = Math.Max(_displayDuration - 0.6, 1.0);
        MainFile.Logger.Info($"ChatBubbleWithHoverTips.OnAnimateInFinished: _displayDuration={_displayDuration}, displayTime={displayTime}");
        GetTree().CreateTimer(displayTime).Timeout += async () => await AnimateOut();
    }

    private async Task AnimateOut()
    {
        try
        {
            _tween?.Kill();
            _tween = CreateTween();
            _tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
            
            await ToSignal(_tween, Tween.SignalName.Finished);
            
            ClearHoverTip();
            QueueFree();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"AnimateOut failed: {ex}");
            ClearHoverTip();
            QueueFree();
        }
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

    private void ClearHoverTip()
    {
        if (_currentHoverTip != null && _mainLabel != null)
        {
            try
            {
                NHoverTipSet.Remove(_mainLabel);
            }
            catch { }
            _currentHoverTip = null;
        }
    }

    public override void _ExitTree()
    {
        ClearHoverTip();
        _tween?.Kill();
    }
}
