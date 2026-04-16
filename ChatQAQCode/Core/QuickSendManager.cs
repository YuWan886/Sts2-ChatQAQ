using System.Reflection;
using System.Text;
using Godot;

namespace ChatQAQ.ChatQAQCode.Core;

public class QuickSendManager : IDisposable
{
    private static readonly Lazy<QuickSendManager> _instance = new(() => new QuickSendManager());
    public static QuickSendManager Instance => _instance.Value;

    public event Action<string>? OnMessageSent;

    private readonly BBcodeTagHelper _tagHelper;
    private readonly LocalizationManager _localizationManager;
    private bool _disposed = false;

    private QuickSendManager()
    {
        _tagHelper = BBcodeTagHelper.Instance;
        _localizationManager = LocalizationManager.Instance;
    }

    public void SendQuickInfoAtPosition(Vector2 position)
    {
        var message = BuildMessageForHoveredControl();
        
        if (!string.IsNullOrWhiteSpace(message))
        {
            SendMessage(message);
        }
    }

    private string BuildMessageForHoveredControl()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var viewport = sceneTree?.Root?.GetViewport();
        if (viewport == null)
        {
            return string.Empty;
        }

        var hoveredControl = viewport.GuiGetHoveredControl();
        if (hoveredControl == null)
        {
            return string.Empty;
        }

        var controlName = hoveredControl.Name.ToString().ToLower();
        var parent = hoveredControl.GetParent();
        var parentName = parent?.Name.ToString().ToLower() ?? "";
        var className = hoveredControl.GetType().Name.ToLower();

        var message = TryGetCardMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetRelicMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetPotionMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetEnemyPowerMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetEnemyIntentMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetEnemyMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetDrawPileMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        message = TryGetDiscardPileMessage(hoveredControl, controlName, parentName, className);
        if (!string.IsNullOrEmpty(message)) return message;

        return string.Empty;
    }

    private string TryGetCardMessage(Control control, string controlName, string parentName, string className)
    {
        object? card = null;
        string cardSource = "hand";

        if (parentName.StartsWith("nhandcardholder"))
        {
            cardSource = "hand";
            var parent = control.GetParent();
            if (parent != null)
            {
                card = GetPropertyValue(parent, "Card");
                if (card == null)
                {
                    card = GetPropertyValue(parent, "CardModel");
                }
            }
        }
        else if (parentName.StartsWith("gridcardholder"))
        {
            var parent = control.GetParent();
            if (parent != null)
            {
                card = GetPropertyValue(parent, "Card");
                if (card == null)
                {
                    card = GetPropertyValue(parent, "CardModel");
                }

                cardSource = DetermineCardSource(parent);
            }
        }
        else if (parentName.Contains("cardholder") || className.Contains("cardholder"))
        {
            var parent = control.GetParent();
            if (parent != null)
            {
                card = GetPropertyValue(parent, "Card");
                if (card == null)
                {
                    card = GetPropertyValue(parent, "CardModel");
                }
                cardSource = DetermineCardSource(parent);
            }
        }

        if (card == null)
        {
            card = GetPropertyValue(control, "Card");
            if (card == null)
            {
                card = GetPropertyValue(control, "CardModel");
            }
        }

        if (card == null) return string.Empty;

        var cardModel = GetPropertyValue(card, "Model") ?? card;
        var cardId = GetPropertyValue(cardModel, "Id")?.ToString() ?? "";
        var cardTitle = GetPropertyValue(cardModel, "Title");
        var cardName = GetLocStringText(cardTitle);

        if (string.IsNullOrEmpty(cardName)) return string.Empty;

        var cardTag = _tagHelper.CreateCardTag(cardId, cardName);

        return cardSource switch
        {
            "hand" => $"{GetLocalizedString("quick_send.hand_card", "我手牌有")}{cardTag}",
            "draw" => $"{GetLocalizedString("quick_send.draw_card", "我抽牌堆有")}{cardTag}",
            "discard" => $"{GetLocalizedString("quick_send.discard_card", "我弃牌堆有")}{cardTag}",
            "exhaust" => $"{GetLocalizedString("quick_send.exhaust_card", "我消耗堆有")}{cardTag}",
            _ => $"{GetLocalizedString("quick_send.card", "我有")}{cardTag}"
        };
    }

    private string DetermineCardSource(Node cardHolder)
    {
        var current = cardHolder.GetParent();
        var depth = 0;
        var maxDepth = 10;

        while (current != null && depth < maxDepth)
        {
            var name = current.Name.ToString().ToLower();

            if (name.Contains("hand"))
            {
                return "hand";
            }
            if (name.Contains("draw") || name.Contains("deck"))
            {
                return "draw";
            }
            if (name.Contains("discard"))
            {
                return "discard";
            }
            if (name.Contains("exhaust"))
            {
                return "exhaust";
            }

            current = current.GetParent();
            depth++;
        }

        return "draw";
    }

    private string TryGetRelicMessage(Control control, string controlName, string parentName, string className)
    {
        var relic = GetPropertyValue(control, "Relic");
        if (relic == null)
        {
            relic = GetPropertyValue(control, "RelicModel");
        }
        
        if (relic == null && (controlName.Contains("relic") || parentName.Contains("relic") || className.Contains("relic")))
        {
            var parent = control.GetParent();
            if (parent != null)
            {
                relic = GetPropertyValue(parent, "Relic");
                if (relic == null)
                {
                    relic = GetPropertyValue(parent, "RelicModel");
                }
            }
        }

        if (relic == null) return string.Empty;

        var relicModel = GetPropertyValue(relic, "Model") ?? relic;
        var relicId = GetPropertyValue(relicModel, "Id")?.ToString() ?? "";
        var relicTitle = GetPropertyValue(relicModel, "Title");
        var relicName = GetLocStringText(relicTitle);

        if (string.IsNullOrEmpty(relicName)) return string.Empty;

        var localizedTemplate = GetLocalizedString("quick_send.relic", "我有遗物");
        var relicTag = _tagHelper.CreateRelicTag(relicId, relicName);

        return $"{localizedTemplate}{relicTag}";
    }

    private string TryGetPotionMessage(Control control, string controlName, string parentName, string className)
    {
        object? potion = FindPotionInAncestors(control);
        
        if (potion == null) return string.Empty;

        var potionModel = GetPropertyValue(potion, "Model") ?? potion;
        var potionId = GetPropertyValue(potionModel, "Id")?.ToString() ?? "";
        var potionTitle = GetPropertyValue(potionModel, "Title");
        var potionName = GetLocStringText(potionTitle);

        if (string.IsNullOrEmpty(potionName)) return string.Empty;

        var localizedTemplate = GetLocalizedString("quick_send.potion", "我有药水");
        var potionTag = _tagHelper.CreatePotionTag(potionId, potionName);

        return $"{localizedTemplate}{potionTag}";
    }

    private object? FindPotionInAncestors(Control control)
    {
        var current = control;
        var depth = 0;
        var maxDepth = 15;

        while (current != null && depth < maxDepth)
        {
            var potion = GetPropertyValue(current, "Potion");
            if (potion != null)
            {
                return potion;
            }

            potion = GetPropertyValue(current, "PotionModel");
            if (potion != null)
            {
                return potion;
            }

            var type = current.GetType();
            if (type.Name.ToLower().Contains("potion"))
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var value = prop.GetValue(current);
                    if (value != null)
                    {
                        var propTypeName = value.GetType().Name.ToLower();
                        if (propTypeName.Contains("potion") && !propTypeName.Contains("potionslot"))
                        {
                            return value;
                        }
                    }
                }
            }

            current = current.GetParent() as Control;
            depth++;
        }

        return null;
    }

    private string TryGetEnemyPowerMessage(Control control, string controlName, string parentName, string className)
    {
        if (parentName != "power" && !className.Contains("power"))
        {
            return string.Empty;
        }

        var parent = control.GetParent();
        if (parent == null) return string.Empty;

        var power = GetPropertyValue(parent, "Power");
        if (power == null)
        {
            power = GetPropertyValue(parent, "PowerModel");
        }

        if (power == null) return string.Empty;

        var powerModel = GetPropertyValue(power, "Model") ?? power;
        var powerTitle = GetPropertyValue(powerModel, "Title");
        var powerName = GetLocStringText(powerTitle);
        var powerAmount = GetPropertyValue(power, "Amount") as int? ?? 0;

        if (string.IsNullOrEmpty(powerName)) return string.Empty;

        var creature = FindCreatureInAncestors(control);
        var creatureName = GetCreatureName(creature);

        var localizedTemplate = GetLocalizedString("quick_send.enemy_power", "有");
        var powerStack = GetLocalizedString("quick_send.power_stack", "层");

        if (!string.IsNullOrEmpty(creatureName))
        {
            return $"{creatureName}{localizedTemplate}{powerAmount}{powerStack}{powerName}";
        }
        
        return $"{localizedTemplate}{powerAmount}{powerStack}{powerName}";
    }

    private string TryGetEnemyIntentMessage(Control control, string controlName, string parentName, string className)
    {
        if (controlName != "intent" && !className.Contains("intent"))
        {
            return string.Empty;
        }

        var intent = GetPropertyValue(control, "Intent");
        if (intent == null)
        {
            intent = GetPropertyValue(control, "IntentModel");
        }

        if (intent == null)
        {
            var parent = control.GetParent();
            if (parent != null)
            {
                intent = GetPropertyValue(parent, "Intent");
                if (intent == null)
                {
                    intent = GetPropertyValue(parent, "IntentModel");
                }
            }
        }

        if (intent == null) return string.Empty;

        var intentModel = GetPropertyValue(intent, "Model") ?? intent;
        var intentTitle = GetPropertyValue(intentModel, "Title");
        var intentName = GetLocStringText(intentTitle);

        if (string.IsNullOrEmpty(intentName))
        {
            intentName = intent.ToString() ?? "";
        }

        if (string.IsNullOrEmpty(intentName)) return string.Empty;

        var creature = FindCreatureInAncestors(control);
        var creatureName = GetCreatureName(creature);
        var intentFormat = GetLocalizedString("quick_send.intent_format", "[{0}]");
        var formattedIntent = string.Format(intentFormat, intentName);

        if (!string.IsNullOrEmpty(creatureName))
        {
            return $"{creatureName}{formattedIntent}";
        }
        
        return formattedIntent;
    }

    private string GetCreatureName(object? creature)
    {
        if (creature == null) return "";

        var model = GetPropertyValue(creature, "Model");
        string name = "";

        if (model != null)
        {
            var title = GetPropertyValue(model, "Title");
            name = GetLocStringText(title);
        }

        if (string.IsNullOrEmpty(name))
        {
            name = GetPropertyValue(creature, "Name")?.ToString() ?? "";
        }

        return name;
    }

    private string TryGetEnemyMessage(Control control, string controlName, string parentName, string className)
    {
        object? creature = FindCreatureInAncestors(control);
        
        if (creature == null) return string.Empty;

        string name = "";
        
        var model = GetPropertyValue(creature, "Model");
        if (model != null)
        {
            var title = GetPropertyValue(model, "Title");
            name = GetLocStringText(title);
        }
        
        if (string.IsNullOrEmpty(name))
        {
            name = GetPropertyValue(creature, "Name")?.ToString() ?? "";
        }
        
        if (string.IsNullOrEmpty(name))
        {
            var creatureModel = GetPropertyValue(creature, "CreatureModel");
            if (creatureModel != null)
            {
                var title = GetPropertyValue(creatureModel, "Title");
                name = GetLocStringText(title);
            }
        }

        var hp = GetPropertyValue(creature, "Hp") ?? GetPropertyValue(creature, "CurrentHp");
        var maxHp = GetPropertyValue(creature, "MaxHp");
        var block = GetPropertyValue(creature, "Block");

        if (string.IsNullOrEmpty(name)) return string.Empty;

        var blockText = GetLocalizedString("quick_send.block", "格挡");
        var hpLabel = GetLocalizedString("quick_send.hp_label", "HP");
        var hpFormat = GetLocalizedString("quick_send.enemy_hp_format", "([color=red]{0}[/color]/[color=gold]{1}[/color]{2})");
        var enemyWithBlockFormat = GetLocalizedString("quick_send.enemy_with_block", "{0}, {1}{2}");
        
        var currentHp = hp ?? 0;
        var maxHpValue = maxHp ?? 0;
        var hpText = string.Format(hpFormat, currentHp, maxHpValue, hpLabel);
        
        var blockValue = block != null ? (int)block : 0;
        if (blockValue > 0)
        {
            hpText = string.Format(enemyWithBlockFormat, hpText, blockValue, blockText);
        }
        
        var sb = new StringBuilder();
        sb.Append(name);
        sb.Append(hpText);

        var powers = GetPropertyValue(creature, "Powers");
        if (powers != null)
        {
            var powerList = new List<string>();
            foreach (var power in powers as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (power == null) continue;

                var powerModel = GetPropertyValue(power, "Model") ?? power;
                var powerTitle = GetPropertyValue(powerModel, "Title");
                var powerName = GetLocStringText(powerTitle);
                var powerAmount = GetPropertyValue(power, "Amount") as int? ?? 0;

                if (!string.IsNullOrEmpty(powerName))
                {
                    var powerType = GetPropertyValue(powerModel, "Type") ?? GetPropertyValue(power, "Type");
                    var isDebuff = IsDebuffPower(powerType, powerModel);
                    var colorTag = isDebuff ? "[color=red]" : "[color=green]";
                    var powerFormat = GetLocalizedString("quick_send.power_format", "{0}({1})");
                    var formattedPower = string.Format(powerFormat, powerName, powerAmount);
                    powerList.Add($"{colorTag}{formattedPower}[/color]");
                }
            }

            if (powerList.Count > 0)
            {
                var separator = GetLocalizedString("quick_send.power_list_separator", ", ");
                var andMore = GetLocalizedString("quick_send.and_more", "等{0}个");
                sb.Append(" (");
                sb.Append(string.Join(separator, powerList.GetRange(0, Math.Min(powerList.Count, 3))));
                if (powerList.Count > 3)
                {
                    sb.Append(string.Format(andMore, powerList.Count - 3));
                }
                sb.Append(")");
            }
        }

        var localizedTemplate = GetLocalizedString("quick_send.enemy", "");
        return $"{localizedTemplate}{sb}";
    }

    private object? FindCreatureInAncestors(Control control)
    {
        var current = control;
        var depth = 0;
        var maxDepth = 15;

        while (current != null && depth < maxDepth)
        {
            var typeName = current.GetType().Name.ToLower();
            var currentName = current.Name.ToString().ToLower();

            if (typeName.Contains("creature") || typeName.Contains("enemy") || typeName.Contains("monster") ||
                currentName.Contains("creature") || currentName.Contains("enemy") || currentName.Contains("monster"))
            {
                var entity = GetPropertyValue(current, "Entity");
                if (entity != null)
                {
                    return entity;
                }

                var creature = GetPropertyValue(current, "Creature");
                if (creature != null)
                {
                    return creature;
                }
            }

            if (typeName == "ncreature")
            {
                var entity = GetPropertyValue(current, "Entity");
                if (entity != null)
                {
                    return entity;
                }
            }

            current = current.GetParent() as Control;
            depth++;
        }

        return null;
    }

    private string TryGetDrawPileMessage(Control control, string controlName, string parentName, string className)
    {
        if (!controlName.Contains("draw") && !parentName.Contains("draw"))
        {
            return string.Empty;
        }

        var player = GameInfoProvider.Instance.GetLocalPlayer();
        if (player == null) return string.Empty;

        var drawPile = GetPropertyValue(player, "DrawPile");
        if (drawPile == null) return string.Empty;

        var count = 0;
        foreach (var _ in drawPile as IEnumerable<object> ?? Array.Empty<object>())
        {
            count++;
        }

        var localizedTemplate = GetLocalizedString("quick_send.draw_pile", "我抽牌堆有");
        var cardUnit = GetLocalizedString("quick_send.card_unit", "张");
        return $"{localizedTemplate}{count}{cardUnit}牌";
    }

    private string TryGetDiscardPileMessage(Control control, string controlName, string parentName, string className)
    {
        if (!controlName.Contains("discard") && !parentName.Contains("discard"))
        {
            return string.Empty;
        }

        var player = GameInfoProvider.Instance.GetLocalPlayer();
        if (player == null) return string.Empty;

        var discardPile = GetPropertyValue(player, "DiscardPile");
        if (discardPile == null) return string.Empty;

        var count = 0;
        foreach (var _ in discardPile as IEnumerable<object> ?? Array.Empty<object>())
        {
            count++;
        }

        var localizedTemplate = GetLocalizedString("quick_send.discard_pile", "我弃牌堆有");
        var cardUnit = GetLocalizedString("quick_send.card_unit", "张");
        return $"{localizedTemplate}{count}{cardUnit}牌";
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj == null) return null;

        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(obj);
    }

    private static string GetLocStringText(object? locString)
    {
        if (locString == null) return "";

        var type = locString.GetType();
        var method = type.GetMethod("GetFormattedText", BindingFlags.Public | BindingFlags.Instance);
        if (method != null)
        {
            var result = method.Invoke(locString, null);
            return result?.ToString() ?? "";
        }

        return locString.ToString() ?? "";
    }

    private void SendMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            MainFile.Logger.Warn("SendMessage: Content is empty");
            return;
        }

        ChatManager.Instance.SendMessage(content);
        OnMessageSent?.Invoke(content);

        MainFile.Logger.Info($"QuickSendManager: Message sent: {content}");
    }

    private string GetLocalizedString(string key, string defaultValue)
    {
        var localized = _localizationManager.GetUI(key);
        return string.IsNullOrEmpty(localized) || localized == key ? defaultValue : localized;
    }

    private bool IsDebuffPower(object? powerType, object? powerModel)
    {
        if (powerType == null)
        {
            var typeName = powerModel?.GetType().Name.ToLower() ?? "";
            var debuffKeywords = new[] { "vulnerable", "weak", "frail", "poison", "burn", "dexteritydown", "strengthdown", "entangle", "slow", "confused" };
            foreach (var keyword in debuffKeywords)
            {
                if (typeName.ToLower().Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        var typeStr = powerType.ToString()?.ToLower() ?? "";
        return typeStr.Contains("debuff") || typeStr == "1";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
