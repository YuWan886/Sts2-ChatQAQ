using System.Reflection;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ChatQAQ.ChatQAQCode.Core;

public class GameInfoProvider
{
    private static readonly Lazy<GameInfoProvider> _instance = new(() => new GameInfoProvider());
    public static GameInfoProvider Instance => _instance.Value;

    private GameInfoProvider() { }

    public List<CardInfo> GetHandCards()
    {
        var result = new List<CardInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var cardCounts = new Dictionary<string, CardInfo>();
        var hand = GetPropertyValue(player, "Hand");
        if (hand != null)
        {
            foreach (var card in hand as IEnumerable<object> ?? Array.Empty<object>())
            {
                AddCardToResult(card, cardCounts);
            }
        }

        return cardCounts.Values.ToList();
    }

    public List<CardInfo> GetDrawPileCards()
    {
        var result = new List<CardInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var cardCounts = new Dictionary<string, CardInfo>();
        var drawPile = GetPropertyValue(player, "DrawPile");
        if (drawPile != null)
        {
            foreach (var card in drawPile as IEnumerable<object> ?? Array.Empty<object>())
            {
                AddCardToResult(card, cardCounts);
            }
        }

        return cardCounts.Values.ToList();
    }

    public List<CardInfo> GetDiscardPileCards()
    {
        var result = new List<CardInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var cardCounts = new Dictionary<string, CardInfo>();
        var discardPile = GetPropertyValue(player, "DiscardPile");
        if (discardPile != null)
        {
            foreach (var card in discardPile as IEnumerable<object> ?? Array.Empty<object>())
            {
                AddCardToResult(card, cardCounts);
            }
        }

        return cardCounts.Values.ToList();
    }

    public List<CardInfo> GetExhaustPileCards()
    {
        var result = new List<CardInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var cardCounts = new Dictionary<string, CardInfo>();
        var exhaustPile = GetPropertyValue(player, "ExhaustPile");
        if (exhaustPile != null)
        {
            foreach (var card in exhaustPile as IEnumerable<object> ?? Array.Empty<object>())
            {
                AddCardToResult(card, cardCounts);
            }
        }

        return cardCounts.Values.ToList();
    }

    public List<CardInfo> GetDeckCards()
    {
        var result = new List<CardInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var cardCounts = new Dictionary<string, CardInfo>();
        var deck = GetPropertyValue(player, "Deck");
        if (deck != null)
        {
            var cards = GetPropertyValue(deck, "Cards");
            if (cards != null)
            {
                foreach (var card in cards as IEnumerable<object> ?? Array.Empty<object>())
                {
                    AddCardToResult(card, cardCounts);
                }
            }
        }

        return cardCounts.Values.ToList();
    }

    public List<RelicInfo> GetRelics()
    {
        var result = new List<RelicInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var relics = GetPropertyValue(player, "Relics");
        if (relics != null)
        {
            foreach (var relic in relics as IEnumerable<object> ?? Array.Empty<object>())
            {
                var relicModel = GetPropertyValue(relic, "Model") ?? relic;
                var id = GetPropertyValue(relicModel, "Id")?.ToString() ?? "";
                var title = GetPropertyValue(relicModel, "Title");
                var name = GetLocStringText(title);

                result.Add(new RelicInfo
                {
                    Id = id,
                    Name = name
                });
            }
        }

        return result;
    }

    public List<PotionInfo> GetPotions()
    {
        var result = new List<PotionInfo>();
        var player = GetLocalPlayer();
        if (player == null) return result;

        var potions = GetPropertyValue(player, "Potions");
        if (potions != null)
        {
            foreach (var potion in potions as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (potion == null) continue;

                var potionModel = GetPropertyValue(potion, "Model") ?? potion;
                var id = GetPropertyValue(potionModel, "Id")?.ToString() ?? "";
                var title = GetPropertyValue(potionModel, "Title");
                var name = GetLocStringText(title);

                result.Add(new PotionInfo
                {
                    Id = id,
                    Name = name
                });
            }
        }

        return result;
    }

    public List<EnemyInfo> GetEnemies()
    {
        var result = new List<EnemyInfo>();
        var combatState = GetCombatState();
        if (combatState == null) return result;

        var enemies = GetPropertyValue(combatState, "Enemies");
        if (enemies != null)
        {
            foreach (var creature in enemies as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (creature == null) continue;

                var isDead = GetPropertyValue(creature, "IsDead");
                if (isDead is bool dead && dead) continue;

                result.Add(CreateEnemyInfo(creature));
            }
        }

        return result;
    }

    public EnemyInfo? GetEnemyDetails(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId)) return null;

        var combatState = GetCombatState();
        if (combatState == null) return null;

        var enemies = GetPropertyValue(combatState, "Enemies");
        if (enemies != null)
        {
            foreach (var creature in enemies as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (creature == null) continue;

                var isDead = GetPropertyValue(creature, "IsDead");
                if (isDead is bool dead && dead) continue;

                var id = GetPropertyValue(creature, "Id")?.ToString() ?? "";
                if (id == enemyId)
                {
                    return CreateEnemyInfo(creature);
                }
            }
        }

        return null;
    }

    public Player? GetLocalPlayer()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return null;

        foreach (var player in runState.Players)
        {
            if (LocalContext.IsMe(player))
            {
                return player;
            }
        }

        return null;
    }

    private object? GetCombatState()
    {
        var runState = RunManager.Instance?.DebugOnlyGetState();
        if (runState == null) return null;

        return GetPropertyValue(runState, "Combat");
    }

    private void AddCardToResult(object? card, Dictionary<string, CardInfo> cardCounts)
    {
        if (card == null) return;

        var cardModel = GetPropertyValue(card, "Model") ?? card;
        var id = GetPropertyValue(cardModel, "Id")?.ToString() ?? "";
        var title = GetPropertyValue(cardModel, "Title");
        var name = GetLocStringText(title);

        if (cardCounts.TryGetValue(id, out var existingCard))
        {
            existingCard.Count++;
        }
        else
        {
            cardCounts[id] = new CardInfo
            {
                Id = id,
                Name = name,
                Count = 1
            };
        }
    }

    private EnemyInfo CreateEnemyInfo(object creature)
    {
        var model = GetPropertyValue(creature, "Model") ?? creature;
        var title = GetPropertyValue(model, "Title");
        var name = GetLocStringText(title);

        var info = new EnemyInfo
        {
            Id = GetPropertyValue(creature, "Id")?.ToString() ?? "",
            Name = name,
            CurrentHp = GetPropertyValue(creature, "Hp") as int? ?? 0,
            MaxHp = GetPropertyValue(creature, "MaxHp") as int? ?? 0,
            Block = GetPropertyValue(creature, "Block") as int? ?? 0,
            Powers = new List<PowerInfo>()
        };

        var powers = GetPropertyValue(creature, "Powers");
        if (powers != null)
        {
            foreach (var power in powers as IEnumerable<object> ?? Array.Empty<object>())
            {
                if (power == null) continue;

                var powerModel = GetPropertyValue(power, "Model") ?? power;
                var powerTitle = GetPropertyValue(powerModel, "Title");
                var powerName = GetLocStringText(powerTitle);

                info.Powers.Add(new PowerInfo
                {
                    Id = GetPropertyValue(powerModel, "Id")?.ToString() ?? "",
                    Name = powerName,
                    Amount = GetPropertyValue(power, "Amount") as int? ?? 0
                });
            }
        }

        return info;
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
}

public class CardInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class RelicInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class PotionInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

public class EnemyInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int Block { get; set; }
    public List<PowerInfo> Powers { get; set; } = new();
}

public class PowerInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Amount { get; set; }
}
