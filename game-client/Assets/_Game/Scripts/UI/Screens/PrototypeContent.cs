using System;

namespace Kingdom.UI
{
    public enum KingdomScreenId
    {
        City,
        World,
        Alliance,
        Mail,
        More,
        Hero,
        Inventory,
        Training,
        Territory,
    }

    internal readonly struct InventoryEntry
    {
        public InventoryEntry(string code, string name, string category, int quantity, int rarity)
        {
            Code = code;
            Name = name;
            Category = category;
            Quantity = quantity;
            Rarity = rarity;
        }

        public string Code { get; }
        public string Name { get; }
        public string Category { get; }
        public int Quantity { get; }
        public int Rarity { get; }
    }

    internal readonly struct MailEntry
    {
        public MailEntry(string code, string title, string detail, int unread)
        {
            Code = code;
            Title = title;
            Detail = detail;
            Unread = unread;
        }

        public string Code { get; }
        public string Title { get; }
        public string Detail { get; }
        public int Unread { get; }
    }

    internal readonly struct AllianceEntry
    {
        public AllianceEntry(string tag, string name, string leader, int members, int capacity, string language, string power)
        {
            Tag = tag;
            Name = name;
            Leader = leader;
            Members = members;
            Capacity = capacity;
            Language = language;
            Power = power;
        }

        public string Tag { get; }
        public string Name { get; }
        public string Leader { get; }
        public int Members { get; }
        public int Capacity { get; }
        public string Language { get; }
        public string Power { get; }
    }

    internal static class PrototypeContent
    {
        public static readonly InventoryEntry[] Inventory =
        {
            new("RS", "Frontier Relocation", "Utility", 3, 3),
            new("SH", "Eight-hour Ward", "Defense", 6, 4),
            new("SP", "Builder's Insight", "Speed", 12, 2),
            new("FR", "Field Rations", "Resource", 38, 1),
            new("OK", "Oathkeeper Token", "Alliance", 4, 4),
            new("ST", "Stonemason Kit", "Resource", 21, 2),
            new("MT", "Moonsteel", "Crafting", 9, 4),
            new("WL", "Winter Lumber", "Resource", 42, 2),
            new("CR", "Astral Compass", "Utility", 1, 5),
            new("SC", "Scout's Cipher", "Combat", 15, 3),
            new("DV", "Dawn Vial", "Recovery", 8, 3),
            new("BN", "Beacon Core", "Event", 2, 5),
        };

        public static readonly MailEntry[] Mail =
        {
            new("MS", "Messages", "Direct messages from other rulers", 4),
            new("AL", "Alliance", "Council notices and alliance mail", 12),
            new("BR", "Battle reports", "Combat outcomes and replays", 2),
            new("EV", "Events", "Milestones and event rewards", 3),
            new("SY", "System", "Account and kingdom announcements", 1),
            new("GA", "Gathering reports", "Completed resource expeditions", 5),
            new("MO", "Monster reports", "Hunt results and loot", 0),
        };

        public static readonly AllianceEntry[] Alliances =
        {
            new("AVR", "Averon Pact", "Mira Vale", 38, 50, "English", "8.4M"),
            new("NTH", "Northwatch", "Oren Pike", 44, 60, "All languages", "11.2M"),
            new("SOL", "Solstice Guard", "Ilya Dawn", 29, 50, "English / Hindi", "6.9M"),
            new("TID", "Tidebound", "Cass Renn", 17, 40, "English", "4.6M"),
        };

        public static readonly string[] AllianceActions =
        {
            "Territory",
            "Rallies",
            "Alliance help",
            "Missions",
            "Research",
            "Members",
        };

        public static readonly string[] UnitRanks =
        {
            "I  Trailguard",
            "III  Pike Warden",
            "V  Aegis Veteran",
            "VII  Dawn Sentinel",
        };
    }
}
