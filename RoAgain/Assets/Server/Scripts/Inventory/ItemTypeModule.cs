using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Represents a type of item, providing access to various of the properties that never change for individual stacks
    /// </summary>
    public class ItemType : IAutoInitPoolObject
    {
        public long TypeId;
        public long BaseTypeId = ItemConstants.BASETYPEID_NONE;
        public bool CanStack;
        public int Weight;
        public int SellPrice;
        public int NumTotalCardSlots;
        public ItemUsageMode UsageMode;
        public int VisualId;
        public LocalizedStringId NameLocId = LocalizedStringId.INVALID;
        public LocalizedStringId FlavorLocId = LocalizedStringId.INVALID;


        private Dictionary<ModifierType, int> _modifiers;
        public IReadOnlyDictionary<ModifierType, int> ReadOnlyModifiers => _modifiers;

        public bool HasAnyModifiers()
        {
            return _modifiers == null || _modifiers.Count == 0;
        }

        public bool HasModifier(ModifierType modifierType)
        {
            if(!HasAnyModifiers())
                return false;

            if (modifierType == ModifierType.Unknown)
                return false;

            return _modifiers != null && _modifiers.ContainsKey(modifierType);
        }

        public int GetModifierValue(ModifierType modifierType)
        {
            if(!HasModifier(modifierType))
                return 0;

            return _modifiers[modifierType];
        }

        public void AddModifier(ModifierType modifierType, int value)
        {
            _modifiers ??= new();

            _modifiers[modifierType] = value;
        }

        public bool Equals(ItemType other)
        {
            if (other == null)
                return false;

            if (other.BaseTypeId != BaseTypeId)
                return false;

            return ModifiersMatchExact(other._modifiers);
        }

        public bool ModifiersMatchExact(Dictionary<ModifierType, int> targetModifiers)
        {
            bool ownModsEmpty = _modifiers == null ||  _modifiers.Count == 0;
            bool targetModsEmpty = targetModifiers == null || targetModifiers.Count == 0;
            if (ownModsEmpty != targetModsEmpty)
                return false;

            if (ownModsEmpty && targetModsEmpty) // Ensure both lists aren't empty for code below
                return true;

            if (targetModifiers.Count != _modifiers.Count)
                return false;

            foreach (var kvp in _modifiers)
            {
                if (!targetModifiers.ContainsKey(kvp.Key))
                    return false;

                if (targetModifiers[kvp.Key] != kvp.Value)
                    return false;
            }

            return true;
        }

        public bool IsValid()
        {
            return TypeId >= 0
                && (BaseTypeId >= 0 || BaseTypeId == ItemConstants.BASETYPEID_NONE)
                && Weight >= 0
                && SellPrice >= 0;
        }

        public virtual ItemTypePacket ToPacket()
        {
            return new()
            {
                TypeId = TypeId,
                BaseTypeId = BaseTypeId,
                Weight = Weight,
                NumTotalCardSlots = NumTotalCardSlots,
                UsageMode = UsageMode,
                VisualId = VisualId,
                NameLocId = NameLocId,
                FlavorLocId = FlavorLocId,
                Modifiers = new(ReadOnlyModifiers)
            };
        }

        public virtual void Reset()
        {
            TypeId = 0;
            BaseTypeId = ItemConstants.BASETYPEID_NONE;
            CanStack = false;
            Weight = 0;
            SellPrice = 0;
            NumTotalCardSlots = 0;
            UsageMode = ItemUsageMode.Unusable;
            VisualId = 0;
            NameLocId = LocalizedStringId.INVALID;
            FlavorLocId = LocalizedStringId.INVALID;
        }
    }

    public class EquippableItemType : ItemType, IAutoInitPoolObject
    {
        public Dictionary<EquipmentSlot, List<IBattleEntityCriterium>> SlotCriteriums = new();

        public List<SimpleStatEntry> SimpleStatEntries = new();
        public List<ConditionalStatEntry> ConditionalStatEntries = new();

        public int EquipScript;
        public int UnequipScript;
        public EquipmentType EquipmentType;

        public bool CanEquip(ServerBattleEntity bEntity)
        {
            foreach (var slotKvp in SlotCriteriums)
            {
                bool slotValid = true;
                foreach (IBattleEntityCriterium criterium in slotKvp.Value)
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

        public HashSet<EquipmentSlot> GetEquippableSlots(ServerBattleEntity bEntity)
        {
            HashSet<EquipmentSlot> slots = new();
            foreach (var slotKvp in SlotCriteriums)
            {
                bool slotValid = true;
                foreach (IBattleEntityCriterium criterium in slotKvp.Value)
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
            EquippableItemTypePacket packet = new()
            {
                TypeId = TypeId,
                BaseTypeId = BaseTypeId,
                Weight = Weight,
                NumTotalCardSlots = NumTotalCardSlots,
                UsageMode = UsageMode,
                VisualId = VisualId,
                NameLocId = NameLocId,
                FlavorLocId = FlavorLocId,
                Modifiers = new(ReadOnlyModifiers),
                SlotCriteriums = new(),
                EquipmentType = EquipmentType
            };

            Dictionary<EquipmentSlot, string> data = new();
            foreach (var kvp in SlotCriteriums)
            {
                string criteriumList = "";
                foreach (IBattleEntityCriterium criterium in kvp.Value)
                {
                    criteriumList += criterium.Serialize();
                }
                data.Add(kvp.Key, criteriumList);
            }
            packet.SlotCriteriums.FromDict(data);
            foreach(SimpleStatEntry entry in SimpleStatEntries)
            {
                packet.SimpleStatStrings.Add(entry.Serialize());
            }
            foreach(ConditionalStatEntry entry in ConditionalStatEntries)
            {
                packet.ConditionalStatStrings.Add(entry.Serialize());
            }

            return packet;
        }

        public override void Reset()
        {
            base.Reset();
            SlotCriteriums.Clear();
            SimpleStatEntries.Clear();
            ConditionalStatEntries.Clear();
            EquipScript = 0;
            UnequipScript = 0;
        }
    }

    public class ConsumableItemType : ItemType, IAutoInitPoolObject
    {
        public int UseScriptId;
        public List<IBattleEntityCriterium> UsageCriteriums = new();

        public override ItemTypePacket ToPacket()
        {
            ConsumableItemTypePacket packet = new()
            {
                TypeId = TypeId,
                BaseTypeId = BaseTypeId,
                Weight = Weight,
                NumTotalCardSlots = NumTotalCardSlots,
                UsageMode = UsageMode,
                VisualId = VisualId,
                NameLocId = NameLocId,
                FlavorLocId = FlavorLocId,
                Modifiers = new(ReadOnlyModifiers),
            };

            packet.UsageCriteriumsList = "";
            foreach (IBattleEntityCriterium criterium in UsageCriteriums)
            {
                packet.UsageCriteriumsList += criterium.Serialize();
            }

            return packet;
        }

        public override void Reset()
        {
            base.Reset();
            UseScriptId = 0;
            UsageCriteriums.Clear();
        }
    }

    /// <summary>
    /// Handles ItemTypes, as well as resolving an itemTypeId to its object
    /// </summary>
    public class ItemTypeModule
    {
        private AItemTypeDatabase _itemTypeDb;

        private Dictionary<long, ItemType> _itemTypeCache = new();
        private Dictionary<long, List<ItemType>> _itemTypesByBaseTypeId = new();
        private Dictionary<long, int> _itemTypeUsageCount = new();

        private Dictionary<int, HashSet<long>> _knownItemTypesByPlayerEntity = new();

        public int Initialize(AItemTypeDatabase itemTypeDb)
        {
            if(itemTypeDb == null)
            {
                OwlLogger.LogError("Can't initialize ItemTypeModule with null ItemTypeDatabase!", GameComponent.Items);
                return -1;
            }

            _itemTypeDb = itemTypeDb;

            return 0;
        }

        public void Shutdown()
        {
            _itemTypeDb = null;

            _itemTypeCache.Clear();
            _itemTypesByBaseTypeId.Clear();
        }

        public ItemType GetOrLoadItemType(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            if(baseTypeId <= 0)
            {
                OwlLogger.LogError($"Can't get ItemType for invalid baseTypeId {baseTypeId}!", GameComponent.Items);
                return null;
            }

            if(modifiers == null || modifiers.Count == 0)
            {
                return GetOrLoadItemType(baseTypeId);
            }

            // Check cache of loaded itemTypes
            if(_itemTypesByBaseTypeId.ContainsKey(baseTypeId))
            {
                List<ItemType> candidateTypes = _itemTypesByBaseTypeId[baseTypeId];
                foreach (ItemType candidateType in candidateTypes)
                {
                    if (candidateType.ModifiersMatchExact(modifiers))
                        return candidateType;
                }
            }

            // Check Db if a matching ItemType can be loaded
            long dbTypeId = _itemTypeDb.GetMatchingItemTypeIdExact(baseTypeId, modifiers);
            if (dbTypeId != ItemConstants.ITEM_TYPE_ID_INVALID)
            {
                return GetOrLoadItemType(dbTypeId);
            }

            // Create a matching ItemType
            return CreateItemType(baseTypeId, modifiers);
        }

        private ItemType CreateItemType(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            ItemType newType = _itemTypeDb.CreateItemType(baseTypeId, modifiers);
            if(newType == null)
            {
                OwlLogger.LogError($"Failed to create new ItemType for baseTypeId {baseTypeId}", GameComponent.Items);
                return null;
            }

            AddItemTypeToCaches(newType);
            return newType;
        }

        public ItemType GetOrLoadItemType(long itemTypeId)
        {
            if (itemTypeId <= 0)
            {
                OwlLogger.LogError($"Can't get ItemType for invalid itemTypeId {itemTypeId}!", GameComponent.Items);
                return null;
            }

            if(_itemTypeCache.ContainsKey(itemTypeId))
            {
                return _itemTypeCache[itemTypeId];
            }

            ItemType loadedType = _itemTypeDb.LoadItemType(itemTypeId);
            if(loadedType == null)
            {
                OwlLogger.LogError($"Tried to load ItemType {itemTypeId} from Db, but wasn't found!", GameComponent.Items);
                return null;
            }

            AddItemTypeToCaches(loadedType);

            return loadedType;
        }

        private void AddItemTypeToCaches(ItemType type)
        {
            _itemTypeCache[type.TypeId] = type;
            if (!_itemTypesByBaseTypeId.ContainsKey(type.BaseTypeId))
                _itemTypesByBaseTypeId[type.BaseTypeId] = new();
            _itemTypesByBaseTypeId[type.BaseTypeId].Add(type);
            _itemTypeUsageCount[type.TypeId] = 0;
        }

        private void RemoveItemTypeFromCaches(ItemType type)
        {
            // Intentionally write this function to not rely on cache-synchronity, so we can use it as an error correction tool
            _itemTypeCache.Remove(type.TypeId);
            if(_itemTypesByBaseTypeId.ContainsKey(type.BaseTypeId))
            {
                _itemTypesByBaseTypeId[type.BaseTypeId].Remove(type);
            }
            _itemTypeUsageCount.Remove(type.TypeId);

            if (type is EquippableItemType equipType)
                AutoInitResourcePool<EquippableItemType>.Return(equipType);
            //else if (type is UsableItemType)
            //    AutoInitResourcePool<UsableItemType>.Return(type);
            else
                AutoInitResourcePool<ItemType>.Return(type);
        }

        public void NotifyItemTypeUsed(long itemTypeId)
        {
            if (!_itemTypeUsageCount.ContainsKey(itemTypeId))
            {
                _itemTypeUsageCount[itemTypeId] = 1;
                return;
            }

            _itemTypeUsageCount[itemTypeId]++;
        }

        public void NotifyItemTypeUseEnded(long itemTypeId)
        {
            if (!_itemTypeUsageCount.ContainsKey(itemTypeId)
                || _itemTypeUsageCount[itemTypeId] <= 0)
            {
                OwlLogger.LogError($"UsageCount for ItemType {itemTypeId} is invalid!", GameComponent.Items);
                return;
            }

            _itemTypeUsageCount[itemTypeId]--;
            if(_itemTypeUsageCount[itemTypeId] <= 0)
            {
                RemoveItemTypeFromCaches(_itemTypeCache[itemTypeId]);
            }
        }

        // Unclear - use only currently loaded types, or go to ItemTypeDb to find _all_ types that're currently created?
        // TODO: Decide this based on where this function will be used.
        public List<ItemType> FindAllItemTypesForBase(long baseTypeId)
        {
            return null;
        }

        private bool IsItemTypeKnownToCharacter(CharacterRuntimeData character, long itemTypeId)
        {
            return _knownItemTypesByPlayerEntity.ContainsKey(character.Id)
                && _knownItemTypesByPlayerEntity[character.Id].Contains(itemTypeId);
        }

        public int SendItemTypeDataToCharacterIfUnknown(CharacterRuntimeData character, long itemTypeId)
        {
            if (IsItemTypeKnownToCharacter(character, itemTypeId))
                return 0;

            ItemType type = GetOrLoadItemType(itemTypeId);
            if (type == null)
                return -1;

            return SendItemTypeDataToCharacterIfUnknown(character, type);
        }

        public int SendItemTypeDataToCharacterIfUnknown(CharacterRuntimeData character, ItemType type)
        {
            if (IsItemTypeKnownToCharacter(character, type.TypeId))
                return 0;

            int result = 0;
            if (type.BaseTypeId != ItemConstants.BASETYPEID_NONE)
            {
                result += SendItemTypeDataToCharacterIfUnknown(character, type.BaseTypeId);
            }

            if (type.HasAnyModifiers())
            {
                if (type.HasModifier(ModifierType.CardSlot_1))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CardSlot_1));
                }

                if (type.HasModifier(ModifierType.CardSlot_2))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CardSlot_2));
                }

                if (type.HasModifier(ModifierType.CardSlot_3))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CardSlot_3));
                }

                if (type.HasModifier(ModifierType.CardSlot_4))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CardSlot_4));
                }

                if (type.HasModifier(ModifierType.CraftingAdditive_1))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CraftingAdditive_1));
                }

                if (type.HasModifier(ModifierType.CraftingAdditive_2))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CraftingAdditive_2));
                }

                if (type.HasModifier(ModifierType.CraftingAdditive_3))
                {
                    result += SendItemTypeDataToCharacterIfUnknown(character, type.GetModifierValue(ModifierType.CraftingAdditive_3));
                }

                // Other modifiers that refer to itemtypes here
            }

            Packet packet = type.ToPacket();
            result += character.Connection.Send(packet);
            if (result == 0)
            {
                if (!_knownItemTypesByPlayerEntity.ContainsKey(character.Id))
                    _knownItemTypesByPlayerEntity[character.Id] = new();
                _knownItemTypesByPlayerEntity[character.Id].Add(type.TypeId);
            }
            return result;
        }
    }
}