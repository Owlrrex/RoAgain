using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    public class EquipmentSetPersistent
    {
        public DictionarySerializationWrapper<EquipmentSlot, long> EquippedTypes = new();
    }

    // Can create subclasses of this type to store per-equipslot data like autocast-cooldowns, 
    public class EquipmentSetEntry
    {
        public ItemType ItemType;
    }

    public class EquipmentSetRuntime
    {
        public Dictionary<EquipmentSlot, EquipmentSetEntry> EquippedTypes;
    }

    /// <summary>
    /// Provides functions to manipulate what items characters have equipped.
    /// </summary>
    public class EquipmentModule
    {
        private InventoryModule _invModule;
        private ItemTypeModule _itemTypeModule;

        public int Initialize(InventoryModule invModule, ItemTypeModule itemTypeModule) // InvModule: To check item ownership ItemTypeModule: To resolve equipment effects
        {
            if(invModule == null)
            {
                OwlLogger.LogError("Can't initialize EquipmentModule with null InventoryModule!", GameComponent.Items);
                return -1;
            }

            if (itemTypeModule == null)
            {
                OwlLogger.LogError("Can't initialize EquipmentModule with null ItemTypeModule!", GameComponent.Items);
                return -1;
            }

            _invModule = invModule;
            _itemTypeModule = itemTypeModule;

            return 0;
        }

        public void Shutdown()
        {
            _invModule = null;
            _itemTypeModule = null;
        }

        public int Equip(CharacterRuntimeData character, EquipmentSlot slot, long itemTypeId)
        {
            return 0;
        }

        public int Equip(EquipmentSetRuntime equipSet, EquipmentSlot slot, long itemTypeId)
        {
            return 0;
        }

        public int Unequip(CharacterRuntimeData character, EquipmentSlot slot)
        {
            return 0;
        }

        public int Unequip(EquipmentSetRuntime equipSet, EquipmentSlot slot)
        {
            return 0;
        }

        private void ApplyPermanentStats(ServerBattleEntity owner, EquippableItemType itemType)
        {

        }

        private void RemovePermanentStats(ServerBattleEntity owner, EquippableItemType itemType)
        {

        }

        private void UpdateSetBonuses()
        {
            // TODO
        }
    }

    public class SimpleStatEntry
    {
        public EntityPropertyType Type;
        public Stat Change;
    }

    public class ConditionalStatEntry
    {
        public EntityPropertyType Type;
        public ConditionalStat ConditionalChange;
    }

    public class EquippableItemType : ItemType, IAutoInitPoolObject
    {
        Dictionary<EquipmentSlot, List<BattleEntityCriterium>> SlotCriteriums = new();

        List<SimpleStatEntry> SimpleStatEntries = new();
        List<ConditionalStatEntry> ConditionalStatEntries = new();

        public int OnEquipScript;
        public int OnUnequipScript;

        public bool CanEquip(ServerBattleEntity bEntity)
        {
            foreach (var slotKvp in SlotCriteriums)
            {
                bool slotValid = true;
                foreach (BattleEntityCriterium criterium in slotKvp.Value)
                {
                    if (!criterium.Evaluate(bEntity))
                    {
                        slotValid = false;
                        break;
                    }
                }
                if (slotValid)
                    return true;
            }

            return false;
        }

        public List<EquipmentSlot> GetEquippableSlots(ServerBattleEntity bEntity)
        {
            List<EquipmentSlot> slots = new();
            foreach (var slotKvp in SlotCriteriums)
            {
                bool slotValid = true;
                foreach (BattleEntityCriterium criterium in slotKvp.Value)
                {
                    if (!criterium.Evaluate(bEntity))
                    {
                        slotValid = false;
                        break;
                    }
                }
                if (slotValid)
                    slots.Add(slotKvp.Key);
            }

            return slots;
        }

        public override ItemTypePacket ToPacket()
        {
            EquippableItemTypePacket packet = new EquippableItemTypePacket()
            {
                TypeId = TypeId,
                BaseTypeId = BaseTypeId,
                Weight = Weight,
                RequiredLevel = RequiredLevel,
                RequiredJobs = RequiredJobs,
                NumTotalCardSlots = NumTotalCardSlots,
                UsageMode = UsageMode,
                VisualId = VisualId,
                NameLocId = NameLocId,
                FlavorLocId = FlavorLocId,
                Modifiers = new(ReadOnlyModifiers),
                SlotCriteriums = new()
            };

            Dictionary<EquipmentSlot, string> data = new();
            foreach (var kvp in SlotCriteriums)
            {
                string criteriumList = "";
                foreach(BattleEntityCriterium criterium in kvp.Value)
                {
                    criteriumList += criterium.Serialize();
                }
                data.Add(kvp.Key, criteriumList);
            }
            packet.SlotCriteriums.FromDict(data);

            return packet;
        }

        public override void Reset()
        {
            base.Reset();
            SlotCriteriums.Clear();
        }
    }
}
