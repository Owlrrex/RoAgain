using System;
using Shared;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Persisted data for a single Item Type
    /// </summary>
    [Serializable]
    public class ItemTypePersistentData
    {
        public long TypeId;
        public long BaseTypeId;
        public bool CanStack;
        public int Weight;
        public int SellPrice;
        public int RequiredLevel;
        public JobFilter RequiredJobs;
        public int NumTotalCardSlots;
        public ItemUsageMode UsageMode;
        public int OnUseScriptId;
        public LocalizedStringId NameLocId;
        public LocalizedStringId FlavorLocId;
        public int VisualId;
        public DictionarySerializationWrapper<ModifierType, int> Modifiers;

        public bool IsValid()
        {
            return TypeId >= 0
                && (BaseTypeId >= 0 || BaseTypeId == ItemConstants.BASETYPEID_NONE)
                && Weight >= 0
                && SellPrice >= 0;
        }

        public bool ModifiersMatchExact(Dictionary<ModifierType, int> targetModifiers)
        {
            bool ownModsEmpty = Modifiers == null || Modifiers.entries == null || Modifiers.entries.Count == 0;
            bool targetModsEmpty = targetModifiers == null || targetModifiers.Count == 0;
            if (ownModsEmpty != targetModsEmpty)
                return false;

            if (ownModsEmpty && targetModsEmpty) // Ensure both lists aren't empty for code below
                return true;

            if (targetModifiers.Count != Modifiers.entries.Count)
                return false;

            foreach (var entry in Modifiers.entries)
            {
                if (!targetModifiers.ContainsKey(entry.key))
                    return false;

                if (targetModifiers[entry.key] != entry.value)
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Persisted data for a single equippable item (including ammo)
    /// </summary>
    public class EquippableTypePersistentData : ItemTypePersistentData
    {

    }

    /// <summary>
    /// Persisted data for a single usable item (excluding equipment & ammo)
    /// </summary>
    public class UsableTypePersistentData : ItemTypePersistentData
    {

    }
}