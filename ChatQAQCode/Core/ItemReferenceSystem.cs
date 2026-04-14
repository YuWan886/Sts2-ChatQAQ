using System;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;

namespace ChatQAQ.ChatQAQCode.Core;

public partial class ItemReferenceSystem : RefCounted
{
    private static readonly Lazy<ItemReferenceSystem> _instance = new(() => new ItemReferenceSystem());
    public static ItemReferenceSystem Instance => _instance.Value;

    public ItemReference? ResolveCardReference(string cardIdOrName)
    {
        if (string.IsNullOrEmpty(cardIdOrName))
        {
            return null;
        }

        CardModel? card = null;

        foreach (var c in ModelDb.AllCards)
        {
            if (string.Equals(c.Id.Entry, cardIdOrName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Id.ToString(), cardIdOrName, StringComparison.OrdinalIgnoreCase))
            {
                card = c;
                break;
            }
        }

        if (card == null)
        {
            foreach (var c in ModelDb.AllCards)
            {
                var title = c.Title;
                if (string.Equals(title, cardIdOrName, StringComparison.OrdinalIgnoreCase))
                {
                    card = c;
                    break;
                }
            }
        }

        if (card == null)
        {
            return null;
        }

        return new ItemReference
        {
            Type = ItemType.Card,
            Id = card.Id.ToString(),
            Name = card.Title,
            Rarity = card.Rarity.ToString(),
            CardModel = card
        };
    }

    public ItemReference? ResolvePotionReference(string potionIdOrName)
    {
        if (string.IsNullOrEmpty(potionIdOrName))
        {
            return null;
        }

        PotionModel? potion = null;

        foreach (var p in ModelDb.AllPotions)
        {
            if (string.Equals(p.Id.Entry, potionIdOrName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Id.ToString(), potionIdOrName, StringComparison.OrdinalIgnoreCase))
            {
                potion = p;
                break;
            }
        }

        if (potion == null)
        {
            foreach (var p in ModelDb.AllPotions)
            {
                var title = p.Title.GetFormattedText();
                if (string.Equals(title, potionIdOrName, StringComparison.OrdinalIgnoreCase))
                {
                    potion = p;
                    break;
                }
            }
        }

        if (potion == null)
        {
            return null;
        }

        return new ItemReference
        {
            Type = ItemType.Potion,
            Id = potion.Id.ToString(),
            Name = potion.Title.GetFormattedText(),
            Rarity = potion.Rarity.ToString(),
            PotionModel = potion
        };
    }

    public ItemReference? ResolveRelicReference(string relicIdOrName)
    {
        if (string.IsNullOrEmpty(relicIdOrName))
        {
            return null;
        }

        RelicModel? relic = null;

        foreach (var r in ModelDb.AllRelics)
        {
            if (string.Equals(r.Id.Entry, relicIdOrName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Id.ToString(), relicIdOrName, StringComparison.OrdinalIgnoreCase))
            {
                relic = r;
                break;
            }
        }

        if (relic == null)
        {
            foreach (var r in ModelDb.AllRelics)
            {
                var title = r.Title.GetFormattedText();
                if (string.Equals(title, relicIdOrName, StringComparison.OrdinalIgnoreCase))
                {
                    relic = r;
                    break;
                }
            }
        }

        if (relic == null)
        {
            return null;
        }

        return new ItemReference
        {
            Type = ItemType.Relic,
            Id = relic.Id.ToString(),
            Name = relic.Title.GetFormattedText(),
            Rarity = relic.Rarity.ToString(),
            RelicModel = relic
        };
    }

    public IHoverTip? GetCardHoverTip(string cardId)
    {
        var card = FindCardById(cardId);
        if (card == null) return null;
        return HoverTipFactory.FromCard(card);
    }

    public IHoverTip? GetPotionHoverTip(string potionId)
    {
        var potion = FindPotionById(potionId);
        if (potion == null) return null;
        return HoverTipFactory.FromPotion(potion);
    }

    public IEnumerable<IHoverTip>? GetRelicHoverTips(string relicId)
    {
        var relic = FindRelicById(relicId);
        if (relic == null) return null;
        return HoverTipFactory.FromRelic(relic);
    }

    private CardModel? FindCardById(string cardId)
    {
        foreach (var c in ModelDb.AllCards)
        {
            if (string.Equals(c.Id.Entry, cardId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Id.ToString(), cardId, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }
        return null;
    }

    private PotionModel? FindPotionById(string potionId)
    {
        foreach (var p in ModelDb.AllPotions)
        {
            if (string.Equals(p.Id.Entry, potionId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Id.ToString(), potionId, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }
        return null;
    }

    private RelicModel? FindRelicById(string relicId)
    {
        foreach (var r in ModelDb.AllRelics)
        {
            if (string.Equals(r.Id.Entry, relicId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(r.Id.ToString(), relicId, StringComparison.OrdinalIgnoreCase))
            {
                return r;
            }
        }
        return null;
    }

    public enum ItemType
    {
        Card,
        Potion,
        Relic
    }

    public partial class ItemReference : RefCounted
    {
        public ItemType Type { get; set; }
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Rarity { get; set; } = "";
        public CardModel? CardModel { get; set; }
        public PotionModel? PotionModel { get; set; }
        public RelicModel? RelicModel { get; set; }

        public string GetDisplayName()
        {
            return Type switch
            {
                ItemType.Card => CardModel?.Title ?? Name,
                ItemType.Potion => PotionModel?.Title.GetFormattedText() ?? Name,
                ItemType.Relic => RelicModel?.Title.GetFormattedText() ?? Name,
                _ => Name
            };
        }
    }
}
