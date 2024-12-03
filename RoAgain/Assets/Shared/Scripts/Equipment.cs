using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Shared
{
    [Flags]
    public enum EquipmentSlot
    {
        Unknown             = 0,
        HeadUpper           = 1 << 0,
        HeadMid             = 1 << 1,
        HeadLower           = 1 << 2,
        Armor               = 1 << 3,
        Garment             = 1 << 4,
        Shoes               = 1 << 5,
        Offhand             = 1 << 6,
        Mainhand            = 1 << 7,
        AccessoryLeft       = 1 << 8,
        AccessoryRight      = 1 << 9,
        Ammo                = 1 << 10,
        CostumeHeadUpper    = 1 << 11,
        CostumeHeadMid      = 1 << 12,
        CostumeHeadLower    = 1 << 13,
        CostumeBack         = 1 << 14,
        MAX = CostumeBack,
        TwoHand             = Offhand | Mainhand,
        HeadUpMid           = HeadUpper | HeadMid,
        HeadLowMid          = HeadMid | HeadLower,
        HeadFull            = HeadUpper | HeadMid | HeadLower,
    }

    public static class EquipmentSlotExtensions
    {
        private static StringBuilder _builder = new();

        public static bool IsSingleSlot(this EquipmentSlot slots)
        {
            return Extensions.IsPowerOfTwo((int)slots);
        }

        public static string ToHumanReadableString(this EquipmentSlot slots)
        {
            _builder.Clear();
            foreach(EquipmentSlot singleSlot in new EquipmentSlotIterator(slots))
            {
                _builder.Append(singleSlot.ToLocStringId().Resolve());
                _builder.Append(" + ");
            }
            _builder.Remove(_builder.Length - 3, 3); // remove last, unnecessary " + "
            return _builder.ToString();
        }

        public static LocalizedStringId ToLocStringId(this EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.HeadUpper => new(227),
                EquipmentSlot.HeadMid => new(228),
                EquipmentSlot.HeadLower => new(229),
                EquipmentSlot.Armor => new(230),
                EquipmentSlot.Garment => new(231),
                EquipmentSlot.Shoes => new(232),
                EquipmentSlot.Offhand => new(233),
                EquipmentSlot.Mainhand => new(234),
                EquipmentSlot.AccessoryLeft => new(235),
                EquipmentSlot.AccessoryRight => new(236),
                EquipmentSlot.Ammo => new(237),
                EquipmentSlot.CostumeHeadUpper => new(238),
                EquipmentSlot.CostumeHeadMid => new(239),
                EquipmentSlot.CostumeHeadLower => new(240),
                EquipmentSlot.CostumeBack => new(241),
                _ => throw new NotImplementedException(),
            };
        }
    }

    /// <summary>
    /// For Equipment types that don't directly relate to their slot
    /// EquipmentSlot criteria can be used to create 1hnd or 2hnd variants, skills can check weapon type & its equip slot
    /// </summary>
    // TODO: Make this a int once I no longer depend on unity's Serializer, to allow more weapon/equip types
    public enum EquipmentType : uint
    {
        Unknown         = 0,
        Unarmed         = 1 << 0,
        Dagger          = 1 << 1,
        Sword           = 1 << 2, 
        Spear           = 1 << 3, 
        Axe             = 1 << 4, 
        Mace            = 1 << 5,
        Staff           = 1 << 6, 
        Bow             = 1 << 7,
        Knuckle         = 1 << 8,
        Instrument      = 1 << 9,
        Whip            = 1 << 10,
        Book            = 1 << 11,
        Katar           = 1 << 12,
        Revolver        = 1 << 13,
        Rifle           = 1 << 14,
        Shotgun         = 1 << 15,
        Gatling         = 1 << 16,
        Grenade         = 1 << 17,
        FuumaShuriken   = 1 << 18,
        Shield          = 1 << 19,
        Armor           = 1 << 20,
        Costume         = 1 << 21,
    }

    public class EquipmentSlotIterator : IEnumerable<EquipmentSlot>
    {
        private EquipmentSlot _slots;

        public EquipmentSlotIterator(EquipmentSlot slots = (EquipmentSlot)(-1))
        {
            _slots = slots;
        }

        public IEnumerator<EquipmentSlot> GetEnumerator()
        {
            for (int i = 1; i <= (int)EquipmentSlot.MAX; i <<= 1)
            {
                if (!_slots.HasFlag((EquipmentSlot)i))
                    continue;
                yield return (EquipmentSlot)i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class SimpleStatEntry
    {
        public EntityPropertyType Type;
        public Stat Change;

        public string Serialize()
        {
            // TODO: Use different serializer for Stats
            return $"({(int)Type},{JsonUtility.ToJson(Change, false)})";
        }

        public static SimpleStatEntry FromString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.StartsWith("(") || !str.EndsWith(")"))
                return null;

            int separatorIdx = str.IndexOf(',');
            return new()
            {
                Type = (EntityPropertyType)int.Parse(str[1..separatorIdx]),
                Change = JsonUtility.FromJson<Stat>(str[(separatorIdx + 1)..^1])
            };
        }
    }

    public class ConditionalStatEntry
    {
        public EntityPropertyType Type;
        public ConditionalStat ConditionalChange;

        public string Serialize()
        {
            return $"({(int)Type},{BecConditionParser.Serialize(ConditionalChange)})";
        }

        public static ConditionalStatEntry FromString(string str, BecConditionParser.ConditionIdResolver idResolver)
        {
            if (idResolver == null)
                return null;

            if (string.IsNullOrEmpty(str))
                return null;

            if (!str.StartsWith("(") || !str.EndsWith(")"))
                return null;

            int separatorIdx = str.IndexOf(',');
            return new()
            {
                Type = (EntityPropertyType)int.Parse(str[1..separatorIdx]),
                ConditionalChange = BecConditionParser.ParseCondStat(str[(separatorIdx + 1)..^1], idResolver)
            };
        }
    }

    public class EquipmentSet<I> where I : class
    {
        // Can create subclasses of this type to store per-equipslot data like autocast-cooldowns, 
        protected class EquipmentSetEntry
        {
            public I ItemType;
            public EquipmentSlot OccupiedSlots;
            // Some form of Set-definition
        }

        protected Dictionary<EquipmentSlot, EquipmentSetEntry> _equippedTypes = new();

        /// <summary>
        /// This event is not broadcasted automatically by the EquipSet so that composite-actions (unequip & re-equip) are only broadcast once
        /// The respective EquipmentModules should broadcast this event as appropriate
        /// Second Paramter = The itemType that was equipped before the change
        /// </summary>
        public Action<EquipmentSlot, I> EquipmentChanged;

        public void SetItemType(EquipmentSlot occupiedSlots, I itemType)
        {
            foreach (EquipmentSlot slot in new EquipmentSlotIterator(occupiedSlots))
            {
                EquipmentSlot groupedSlots = GetGroupedSlots(slot);
                if(!occupiedSlots.HasFlag(groupedSlots))
                {
                    OwlLogger.LogError($"Trying to modify EquipmentSlot {occupiedSlots}, but one of the items is not fully contained in this group ({groupedSlots})! This can cause broken EquipmentSet entries!", GameComponent.Items);
                }

                if (itemType != null)
                {
                    if (!_equippedTypes.ContainsKey(slot))
                        _equippedTypes[slot] = new();

                    _equippedTypes[slot].ItemType = itemType;
                    _equippedTypes[slot].OccupiedSlots = occupiedSlots;
                }
                else
                {
                    _equippedTypes.Remove(slot);
                }
            }
        }

        /// <summary>
        /// Sets the given itemType to all slots that are grouped with any of the provided slots.
        /// </summary>
        /// <param name="occupiedSlots">Slots that should have themselves & their grouped slots set to the itemtype</param>
        /// <param name="itemType">itemtype to set</param>
        /// <returns>Bitmask of the slots that were modified, or already had the desired value</returns>
        public EquipmentSlot SetItemTypeOnGroup(EquipmentSlot occupiedSlots, I itemType)
        {
            EquipmentSlot modifiedSlots = occupiedSlots;
            foreach(EquipmentSlot slot in new EquipmentSlotIterator(occupiedSlots))
            {
                EquipmentSlot groupedSlots = GetGroupedSlots(slot);
                SetItemType(groupedSlots, itemType);
                occupiedSlots |= groupedSlots;
            }
            return modifiedSlots;
        }

        /// <summary>
        /// Gets the ItemType equipped in a given slot
        /// </summary>
        /// <param name="slot">The Slot to query. Has to be a singular EquipmentSlot!</param>
        /// <returns></returns>
        public I GetItemType(EquipmentSlot slot)
        {
            if (!_equippedTypes.ContainsKey(slot))
                return null;

            EquipmentSetEntry entry = _equippedTypes[slot];
            if (entry == null)
                return null;

            return entry.ItemType;
        }

        /// <summary>
        /// Not guaranteed to detect empty slots ("Unarmed" or "Unknown" or null) as grouped
        /// </summary>
        /// <param name="slots">The slots which items must cover</param>
        /// <returns>All slots covered by the items that occupy at least one of the given slots</returns>
        public EquipmentSlot GetGroupedSlots(EquipmentSlot slots)
        {
            if (slots == EquipmentSlot.Unknown)
                return EquipmentSlot.Unknown;

            EquipmentSlot groupedSlots = slots;
            foreach (EquipmentSlot slot in new EquipmentSlotIterator(slots))
            {
                if (!HasItemEquippedInSlot(slot))
                    continue;

                EquipmentSetEntry entry = _equippedTypes[slot];
                if (entry == null)
                    continue;

                groupedSlots |= entry.OccupiedSlots;
            }

            return groupedSlots;
        }

        public bool HasItemEquippedInSlot(EquipmentSlot slot)
        {
            return GetItemType(slot) != null;
        }

        public bool IsDualWielding()
        {
            return HasItemEquippedInSlot(EquipmentSlot.Mainhand) && HasItemEquippedInSlot(EquipmentSlot.Offhand) // Both hands have an item
                && !IsTwoHanding(); // ... and are not grouped together (two-handed weapon)
        }

        public bool IsTwoHanding()
        {
            return GetGroupedSlots(EquipmentSlot.Mainhand).HasFlag(EquipmentSlot.Offhand);
        }
    }
}
