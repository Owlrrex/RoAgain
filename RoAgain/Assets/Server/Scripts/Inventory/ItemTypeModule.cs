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
        public const long BASETYPEID_NONE = ItemTypeDatabase.ITEM_TYPE_ID_INVALID;

        public long TypeId;
        public long BaseTypeId = BASETYPEID_NONE;
        public bool CanStack;
        public int Weight;
        public int SellPrice;
        public int RequiredLevel;
        public JobFilter RequiredJobs;
        public int NumTotalCardSlots;
        public ItemUsageMode UsageMode;
        public int OnUseScript;
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
                && (BaseTypeId >= 0 || BaseTypeId == BASETYPEID_NONE)
                && Weight >= 0
                && SellPrice >= 0;
        }

        public ItemTypePacket ToPacket()
        {
            return new()
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
                Modifiers = new(ReadOnlyModifiers)
            };
        }

        public void Reset()
        {
            TypeId = 0;
            BaseTypeId = BASETYPEID_NONE;
            CanStack = false;
            Weight = 0;
            SellPrice = 0;
            RequiredLevel = 0;
            RequiredJobs = JobFilter.Unknown;
            NumTotalCardSlots = 0;
            UsageMode = ItemUsageMode.Unusable;
            OnUseScript = 0;
            VisualId = 0;
            NameLocId = LocalizedStringId.INVALID;
            FlavorLocId = LocalizedStringId.INVALID;
    }
    }

    /// <summary>
    /// Handles ItemTypes, as well as resolving an itemTypeId to its object
    /// </summary>
    public class ItemTypeModule
    {
        private ItemTypeDatabase _itemTypeDb;

        private Dictionary<long, ItemType> _itemTypeCache = new();
        private Dictionary<long, List<ItemType>> _itemTypesByBaseTypeId = new();
        private Dictionary<long, int> _itemTypeUsageCount = new();

        public int Initialize(ItemTypeDatabase itemTypeDb)
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
            if (dbTypeId != ItemTypeDatabase.ITEM_TYPE_ID_INVALID)
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
            AutoInitResourcePool<ItemType>.Return(type);
        }

        public void NotifyItemStackCreated(ItemStack stack)
        {
            if (!_itemTypeUsageCount.ContainsKey(stack.ItemType.TypeId))
            {
                _itemTypeUsageCount[stack.ItemType.TypeId] = 1;
                return;
            }

            _itemTypeUsageCount[stack.ItemType.TypeId]++;
        }

        public void NotifyItemStackDestroyed(ItemStack stack)
        {
            if (!_itemTypeUsageCount.ContainsKey(stack.ItemType.TypeId)
                || _itemTypeUsageCount[stack.ItemType.TypeId] <= 0)
            {
                OwlLogger.LogError($"UsageCount for ItemType {stack.ItemType.TypeId} is invalid!", GameComponent.Items);
                return;
            }

            _itemTypeUsageCount[stack.ItemType.TypeId]--;
            if(_itemTypeUsageCount[stack.ItemType.TypeId] <= 0)
            {
                RemoveItemTypeFromCaches(stack.ItemType);
            }
        }

        // Unclear - use only currently loaded types, or go to ItemTypeDb to find _all_ types that're currently created?
        // TODO: Decide this based on where this function will be used.
        public List<ItemType> FindAllItemTypesForBase(long baseTypeId)
        {
            return null;
        }
    }
}