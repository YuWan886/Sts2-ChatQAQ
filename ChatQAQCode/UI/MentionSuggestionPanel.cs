using Godot;
using System.Collections.Generic;
using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.Core;

namespace ChatQAQ.ChatQAQCode.UI;

public partial class MentionSuggestionPanel : PopupPanel
{
    [Signal]
    public delegate void PlayerSelectedEventHandler(string playerName);

    private VBoxContainer _itemContainer = null!;
    private readonly List<PlayerInfo> _suggestions = new();
    private readonly List<Button> _itemButtons = new();
    private int _selectedIndex = -1;
    private int _maxVisibleItems = 5;
    private string _currentFilter = "";
    private double _lastInputTime;
    private double _debounceDelay = 0.3;

    public bool IsShowing => Visible && _suggestions.Count > 0;
    public int SelectedIndex => _selectedIndex;

    public override void _Ready()
    {
        OnReady();
    }

    public void OnReady()
    {
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = StsUiStyles.PanelBg;
        styleBox.BorderColor = StsUiStyles.PanelBorder;
        styleBox.SetBorderWidthAll(1);
        styleBox.SetCornerRadiusAll(4);
        styleBox.SetContentMarginAll(4);
        AddThemeStyleboxOverride("panel", styleBox);

        _itemContainer = new VBoxContainer();
        _itemContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _itemContainer.AddThemeConstantOverride("separation", 2);
        AddChild(_itemContainer);

        Visible = false;
    }

    public void ShowSuggestions(string filter, Vector2 position)
    {
        _currentFilter = filter ?? "";
        _lastInputTime = Time.GetTicksMsec() / 1000.0;

        var players = GetFilteredPlayers(_currentFilter);

        if (players.Count == 0)
        {
            Hide();
            return;
        }

        UpdateSuggestionList(players);
        Position = new Vector2I((int)position.X, (int)position.Y);
        Show();
        _selectedIndex = 0;
        UpdateSelection();
    }

    public void UpdateFilter(string filter)
    {
        _currentFilter = filter ?? "";
        _lastInputTime = Time.GetTicksMsec() / 1000.0;

        var players = GetFilteredPlayers(_currentFilter);

        if (players.Count == 0)
        {
            Hide();
            return;
        }

        UpdateSuggestionList(players);

        if (_selectedIndex >= _suggestions.Count)
        {
            _selectedIndex = _suggestions.Count - 1;
        }
        UpdateSelection();
    }

    private List<PlayerInfo> GetFilteredPlayers(string filter)
    {
        var allPlayers = MentionSystem.Instance.OnlinePlayers.Values;
        var filtered = new List<PlayerInfo>();

        foreach (var player in allPlayers)
        {
            if (string.IsNullOrEmpty(filter))
            {
                filtered.Add(player);
                continue;
            }

            bool nameMatches = player.PlayerName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

            if (nameMatches)
            {
                filtered.Add(player);
            }
        }

        filtered.Sort((a, b) =>
        {
            if (string.IsNullOrEmpty(filter)) return string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);

            bool aStartsWith = a.PlayerName.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
            bool bStartsWith = b.PlayerName.StartsWith(filter, StringComparison.OrdinalIgnoreCase);

            if (aStartsWith && !bStartsWith) return -1;
            if (!aStartsWith && bStartsWith) return 1;

            return string.Compare(a.PlayerName, b.PlayerName, StringComparison.OrdinalIgnoreCase);
        });

        if (filtered.Count > _maxVisibleItems)
        {
            filtered = filtered.GetRange(0, _maxVisibleItems);
        }

        return filtered;
    }

    private void UpdateSuggestionList(List<PlayerInfo> players)
    {
        _suggestions.Clear();

        foreach (var child in _itemContainer.GetChildren())
        {
            child.QueueFree();
        }
        _itemButtons.Clear();

        foreach (var player in players)
        {
            _suggestions.Add(player);

            var button = CreateSuggestionButton(player);
            _itemContainer.AddChild(button);
            _itemButtons.Add(button);
        }

        Size = new Vector2I(200, Mathf.Min(_suggestions.Count * 28 + 8, 148));
    }

    private Button CreateSuggestionButton(PlayerInfo player)
    {
        var button = new Button();
        button.Text = player.PlayerName;
        button.CustomMinimumSize = new Vector2(196, 26);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.ToggleMode = true;

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = new Color(0, 0, 0, 0);
        normalStyle.SetContentMarginAll(4);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = StsUiStyles.ButtonHover;
        hoverStyle.SetCornerRadiusAll(3);
        hoverStyle.SetContentMarginAll(4);

        var pressedStyle = new StyleBoxFlat();
        pressedStyle.BgColor = StsUiStyles.Gold;
        pressedStyle.SetCornerRadiusAll(3);
        pressedStyle.SetContentMarginAll(4);

        button.AddThemeStyleboxOverride("normal", normalStyle);
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        button.AddThemeColorOverride("font_color", StsUiStyles.TextPrimary);
        button.AddThemeColorOverride("font_hover_color", StsUiStyles.Cream);
        button.AddThemeColorOverride("font_pressed_color", new Color(0.1f, 0.1f, 0.1f));
        button.AddThemeFontSizeOverride("font_size", 13);

        button.Pressed += () => OnPlayerSelected(player.PlayerName);

        return button;
    }

    private void OnPlayerSelected(string playerName)
    {
        EmitSignal(SignalName.PlayerSelected, playerName);
        Hide();
    }

    public bool NavigateUp()
    {
        if (!Visible || _suggestions.Count == 0) return false;

        _selectedIndex = (_selectedIndex - 1 + _suggestions.Count) % _suggestions.Count;
        UpdateSelection();
        return true;
    }

    public bool NavigateDown()
    {
        if (!Visible || _suggestions.Count == 0) return false;

        _selectedIndex = (_selectedIndex + 1) % _suggestions.Count;
        UpdateSelection();
        return true;
    }

    public bool SelectCurrent()
    {
        if (!Visible || _selectedIndex < 0 || _selectedIndex >= _suggestions.Count) return false;

        var selectedPlayer = _suggestions[_selectedIndex];
        OnPlayerSelected(selectedPlayer.PlayerName);
        return true;
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < _itemButtons.Count; i++)
        {
            _itemButtons[i].ButtonPressed = (i == _selectedIndex);
        }
    }

    public string GetSelectedPlayerName()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _suggestions.Count)
        {
            return _suggestions[_selectedIndex].PlayerName;
        }
        return string.Empty;
    }

    public new void Hide()
    {
        Visible = false;
        _suggestions.Clear();
        _selectedIndex = -1;
    }
}
