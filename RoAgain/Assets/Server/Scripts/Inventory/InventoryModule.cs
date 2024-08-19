using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Provides functions to manipulate Inventories & the Items in them
    /// </summary>
    public class InventoryModule
    {
        private InventoryDatabase _invDb;
        private ItemTypeModule _itemTypeModule;

        private Dictionary<int, Inventory> _cachedInventories = new();

        private Dictionary<int, HashSet<long>> _knownItemTypesByPlayerEntity = new();

        public int Initialize(ItemTypeModule itemTypeModule, InventoryDatabase invDb)
        {
            if(itemTypeModule == null)
            {
                OwlLogger.LogError("Can't initialize InventoryModule with null ItemTypeModule!", GameComponent.Items);
                return -1;
            }

            if(invDb == null)
            {
                OwlLogger.LogError("Can't initialize InventoryModule with null InventoryDatabase!", GameComponent.Items);
                return -1;
            }

            _itemTypeModule = itemTypeModule;
            _invDb = invDb;

            return 0;
        }

        public void Shutdown()
        {
            foreach(var inventory in _cachedInventories.Values)
            {
                foreach(var stack in inventory.ItemStacksByTypeId.Values)
                {
                    DestroyItemStack(stack);
                }
                AutoInitResourcePool<Inventory>.Return(inventory);
            }
            _cachedInventories.Clear();

            _itemTypeModule = null;
            _invDb = null;
        }

        public Inventory CreateInventory()
        {
            InventoryPersistenceData persData = _invDb.CreateInventory();
            if (_cachedInventories.ContainsKey(persData.InventoryId))
            {
                OwlLogger.LogError($"New Inventory was created with Id {persData.InventoryId} that's already cached!", GameComponent.Items);
                return null;
            }

            Inventory inv = PersDataToInventory(persData);
            _cachedInventories.Add(inv.InventoryId, inv);
            return inv;
        }

        public Inventory GetOrLoadInventory(int inventoryId)
        {
            if(_cachedInventories.ContainsKey(inventoryId))
                return _cachedInventories[inventoryId];

            InventoryPersistenceData invPersData = _invDb.LoadInventoryPersistenceData(inventoryId);
            if (invPersData == null)
            {
                OwlLogger.LogError($"Tried to GetOrLoad InventoryId {inventoryId} which doesn't exist!", GameComponent.Items);
                return null;
            }

            Inventory inv = PersDataToInventory(invPersData);
            _cachedInventories.Add(inv.InventoryId, inv);
            return inv;
        }

        public void ClearInventoryFromCache(int inventoryId)
        {
            if (!_cachedInventories.ContainsKey(inventoryId))
                return;

            AutoInitResourcePool<Inventory>.Return(_cachedInventories[inventoryId]);
            _cachedInventories.Remove(inventoryId);
        }

        private Inventory PersDataToInventory(InventoryPersistenceData persData)
        {
            Inventory inventory = AutoInitResourcePool<Inventory>.Acquire();
            inventory.InventoryId = persData.InventoryId;
            inventory.ItemStacksByTypeId = persData.ItemsWrapper.ToDict();
            return inventory;
        }

        private InventoryPersistenceData InventoryToPersData(Inventory inv)
        {
            return new()
            {
                InventoryId = inv.InventoryId,
                ItemsWrapper = new(inv.ItemStacksByTypeId)
            };
        }

        /// <summary>
        /// Creates an ItemStack of a suitable ItemType specified by baseType & modifiers (if any).
        /// </summary>
        /// <param name="baseTypeId"></param>
        /// <param name="count"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        public ItemStack CreateItemStack(long baseTypeId, int count, Dictionary<ModifierType, int> modifiers = null)
        {
            if(baseTypeId <= 0)
            {
                OwlLogger.LogError($"Can't create itemStack with invalid baseTypeId {baseTypeId}", GameComponent.Items);
                return null;
            }

            if(count <= 0)
            {
                OwlLogger.LogError($"Can't create itemStack with invalid itemCount, type {baseTypeId}", GameComponent.Items);
                return null;
            }

            ItemType type = _itemTypeModule.GetOrLoadItemType(baseTypeId, modifiers);
            if(type == null)
            {
                OwlLogger.LogError($"Fetching ItemType failed.", GameComponent.Items);
                return null;
            }

            ItemStack stack = AutoInitResourcePool<ItemStack>.Acquire();
            stack.ItemType = type;
            stack.ItemCount = count;
            _itemTypeModule.NotifyItemStackCreated(stack);

            return stack;
        }

        public void DestroyItemStack(ItemStack stack)
        {
            _itemTypeModule.NotifyItemStackDestroyed(stack);
            AutoInitResourcePool<ItemStack>.Return(stack);
        }

        public int AddItemsToInventory(Inventory inventory, ItemStack stack)
        {
            if(inventory == null)
            {
                OwlLogger.LogError("Can't add items to null inventory!", GameComponent.Items);
                return -1;
            }

            if (stack == null)
            {
                OwlLogger.LogError($"Can't add null ItemStack to Inventory {inventory.InventoryId}!", GameComponent.Items);
                return -1;
            }

            if(stack.ItemType == null || stack.ItemType.TypeId <= 0)
            {
                OwlLogger.LogError($"Can't add item stack with invalid itemTypeId to inventory {inventory.InventoryId}", GameComponent.Items);
                return -2;
            }

            if (stack.ItemCount <= 0)
            {
                OwlLogger.LogError($"Can't add ItemStack with ItemCount 0 to Inventory {inventory.InventoryId}, ItemType {stack.ItemType}", GameComponent.Items);
                return -3;
            }

            if (inventory.HasItemTypeExact(stack.ItemType.TypeId))
            {
                inventory.ItemStacksByTypeId[stack.ItemType.TypeId].ItemCount += stack.ItemCount;
                return 0;
            }

            inventory.ItemStacksByTypeId.Add(stack.ItemType.TypeId, stack);
            return 0;
        }

        public int AddItemsToInventory(Inventory inventory, long itemTypeId, int count)
        {
            if (inventory == null)
            {
                OwlLogger.LogError("Can't add items to null inventory!", GameComponent.Items);
                return -1;
            }

            if(itemTypeId <= 0)
            {
                OwlLogger.LogError("Can't add items with invalid itemTypeId", GameComponent.Items);
                return -2;
            }

            if (count <= 0)
            {
                OwlLogger.LogError($"Can't add ItemStack with ItemCount 0 to Inventory {inventory.InventoryId}, ItemType {itemTypeId}", GameComponent.Items);
                return -3;
            }

            if(inventory.HasItemTypeExact(itemTypeId))
            {
                inventory.ItemStacksByTypeId[itemTypeId].ItemCount += count;
                return 0;
            }

            ItemStack stack = CreateItemStack(itemTypeId, count);
            if (stack == null)
            {
                OwlLogger.LogError("Creating ItemStack failed.", GameComponent.Items);
                return -4;
            }

            return AddItemsToInventory(inventory, stack);
        }

        public int RemoveItemsFromInventory(Inventory inventory, ItemStack stack, bool allowDeleteStack)
        {
            if(inventory == null)
            {
                OwlLogger.LogError("Can't remove items from null inventory!", GameComponent.Items);
                return -1;
            }

            if(stack == null)
            {
                OwlLogger.LogError($"Can't remove null ItemStack from inventory {inventory.InventoryId}", GameComponent.Items);
                return -1;
            }

            return RemoveItemsFromInventory(inventory, stack.ItemType.TypeId, stack.ItemCount, allowDeleteStack);
        }

        public int RemoveItemsFromInventory(Inventory inventory, long itemTypeId, int count, bool allowDeleteStack)
        {
            if (inventory == null)
            {
                OwlLogger.LogError("Can't remove items from null inventory!", GameComponent.Items);
                return -1;
            }

            if (itemTypeId <= 0)
            {
                OwlLogger.LogError("Can't remove items with invalid itemTypeId", GameComponent.Items);
                return -2;
            }

            if (count <= 0)
            {
                OwlLogger.LogError($"Can't remove ItemStack with ItemCount 0 from Inventory {inventory.InventoryId}, ItemType {itemTypeId}", GameComponent.Items);
                return -3;
            }

            if (!inventory.HasItemTypeExact(itemTypeId))
            {
                OwlLogger.LogError($"Inventory {inventory.InventoryId} doesn't contain ItemType {itemTypeId} to remove!", GameComponent.Items);
                return -4;
            }

            ItemStack stack = inventory.ItemStacksByTypeId[itemTypeId];
            if(stack.ItemCount < count)
            {
                OwlLogger.LogError($"Inventory {inventory.InventoryId} doesn't have enough items {itemTypeId} to remove: Has {stack.ItemCount}, required {count}!", GameComponent.Items);
                return -5;
            }

            stack.ItemCount -= count;

            if(stack.ItemCount == 0)
            {
                int removeResult = RemoveItemStackFromInventory(inventory, itemTypeId);
                if (allowDeleteStack)
                {
                    DestroyItemStack(stack);
                }
            }

            return 0;
        }

        private int RemoveItemStackFromInventory(Inventory inventory, long itemTypeId)
        {
            if (inventory == null)
            {
                OwlLogger.LogError("Can't remove itemStack from null inventory!", GameComponent.Items);
                return -1;
            }

            if (itemTypeId <= 0)
            {
                OwlLogger.LogError("Can't remove itemStack with invalid itemTypeId", GameComponent.Items);
                return -2;
            }

            if (!inventory.HasItemTypeExact(itemTypeId))
            {
                OwlLogger.LogError($"Inventory {inventory.InventoryId} doesn't contain ItemType {itemTypeId} to remove Stack!", GameComponent.Items);
                return -3;
            }

            inventory.ItemStacksByTypeId.Remove(itemTypeId);

            // Can't attempt to delete ItemType from ItemTypeModule here - the Stack may be added to another inventory yet
            return 0;
        }

        public bool HasPlayerSpaceForItemStack(CharacterRuntimeData character, ItemStack stack)
        {
            if (stack == null)
            {
                OwlLogger.LogError("ItemStack can't be null!", GameComponent.Items);
                return false;
            }
            return HasPlayerSpaceForItemStack(character, stack.ItemType, stack.ItemCount);
        }

        public bool HasPlayerSpaceForItemStack(CharacterRuntimeData character, long itemTypeId, int count)
        {
            return HasPlayerSpaceForItemStack(character, _itemTypeModule.GetOrLoadItemType(itemTypeId), count);
        }

        public bool HasPlayerSpaceForItemStack(CharacterRuntimeData character, ItemType type, int count)
        {
            if (character == null)
            {
                OwlLogger.LogError("Character can't be null!", GameComponent.Items);
                return false;
            }

            if (type == null)
            {
                OwlLogger.LogError("ItemType can't be null!", GameComponent.Items);
                return false;
            }
            int neededWeight = count * type.Weight;
            return character.CurrentWeight + neededWeight <= character.WeightLimit.Total;
        }

        public int AddItemsToInventory(CharacterRuntimeData character, ItemStack stack)
        {
            if (stack == null)
            {
                OwlLogger.LogError($"Can't add null itemStack to character {(character == null ? "null" : character.Id)}", GameComponent.Items);
                return -1;
            }
                
            return AddItemsToInventory(character, stack.ItemType.TypeId, stack.ItemCount);
        }

        public int AddItemsToInventory(CharacterRuntimeData character, long itemTypeId, int count)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't add items to null character!", GameComponent.Items);
                return -1;
            }

            if(!HasPlayerSpaceForItemStack(character, itemTypeId, count))
            {
                OwlLogger.LogF("Character {0} can't receive {1}x itemType {2} - not enough weight capacity.", character.CharacterId, count, itemTypeId, GameComponent.Items, LogSeverity.Verbose);
                return -2;
            }

            // If implementing ItemStack-limit: Check here

            Inventory inventory = GetOrLoadInventory(character.InventoryId);

            int result = AddItemsToInventory(inventory, itemTypeId, count);

            if(result == 0)
            {
                SendItemStackDataToCharacter(character, itemTypeId, count);

                ItemType type = _itemTypeModule.GetOrLoadItemType(itemTypeId);
                if (type == null)
                {
                    OwlLogger.LogError($"Fetching itemtype {itemTypeId} failed!", GameComponent.Items);
                    return -3;
                }

                if (type.Weight > 0)
                    ModifyCharacterWeight(character, type.Weight * count);
            }    

            return result;
        }

        public int RemoveItemsFromInventory(CharacterRuntimeData character, ItemStack stack, bool allowDeleteStack)
        {
            if (stack == null)
            {
                OwlLogger.LogError($"Can't remove null itemStack from character {(character == null ? "null" : character.Id)}", GameComponent.Items);
                return -1;
            }

            return RemoveItemsFromInventory(character, stack.ItemType.TypeId, stack.ItemCount, allowDeleteStack);
        }

        public int RemoveItemsFromInventory(CharacterRuntimeData character, long itemTypeId, int count, bool allowDeleteStack)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't remove items from null character!", GameComponent.Items);
                return -1;
            }

            Inventory inventory = GetOrLoadInventory(character.InventoryId);

            int result = RemoveItemsFromInventory(inventory, itemTypeId, count, allowDeleteStack);

            if (result == 0)
            {
                Packet packet;
                int remainingCount = inventory.GetItemCountExact(itemTypeId);
                if (remainingCount == 0)
                {
                    packet = new ItemStackRemovedPacket()
                    {
                        InventoryId = character.InventoryId,
                        ItemTypeId = itemTypeId
                    };
                }
                else
                {
                    packet = new ItemStackPacket()
                    {
                        InventoryId = character.InventoryId,
                        ItemTypeId = itemTypeId,
                        ItemCount = remainingCount
                    };
                }

                character.Connection.Send(packet);

                ItemType type = _itemTypeModule.GetOrLoadItemType(itemTypeId);
                if(type == null)
                {
                    OwlLogger.LogError($"Fetching itemtype {itemTypeId} failed!", GameComponent.Items);
                    return -2;
                }

                if(type.Weight > 0)
                   ModifyCharacterWeight(character, -type.Weight * count);
            }

            return result;
        }

        public int SendItemStackDataToCharacter(CharacterRuntimeData character, long itemTypeId, int count)
        {
            SendItemTypeDataToCharacterIfUnknown(character, itemTypeId);

            ItemStackPacket packet = new()
            {
                InventoryId = character.InventoryId,
                ItemTypeId = itemTypeId,
                ItemCount = count
            };

            return character.Connection.Send(packet);
        }

        private bool IsItemTypeKnownToCharacter(CharacterRuntimeData character, long itemTypeId)
        {
            return _knownItemTypesByPlayerEntity.ContainsKey(character.Id)
                && _knownItemTypesByPlayerEntity[character.Id].Contains(itemTypeId);
        }

        private int SendItemTypeDataToCharacterIfUnknown(CharacterRuntimeData character, long itemTypeId)
        {
            if (IsItemTypeKnownToCharacter(character, itemTypeId))
                return 0;

            ItemType type = _itemTypeModule.GetOrLoadItemType(itemTypeId);
            if (type == null)
                return -1;

            return SendItemTypeDataToCharacterIfUnknown(character, type);
        }

        private int SendItemTypeDataToCharacterIfUnknown(CharacterRuntimeData character, ItemType type)
        {
            if (IsItemTypeKnownToCharacter(character, type.TypeId))
                return 0;

            int result = 0;
            if (type.BaseTypeId != ItemType.BASETYPEID_NONE)
            {
                result += SendItemTypeDataToCharacterIfUnknown(character, type.BaseTypeId);
            }
            
            if(type.HasAnyModifiers())
            {
                if(type.HasModifier(ModifierType.CardSlot_1))
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
            if(result == 0)
            {
                if (!_knownItemTypesByPlayerEntity.ContainsKey(character.Id))
                    _knownItemTypesByPlayerEntity[character.Id] = new();
                _knownItemTypesByPlayerEntity[character.Id].Add(type.TypeId);
            }
            return result;
        }

        private void ModifyCharacterWeight(CharacterRuntimeData character, int weightDiff)
        {
            if (weightDiff == 0)
                return;

            int newWeight = character.CurrentWeight + weightDiff;
            if(newWeight < 0 || newWeight > character.WeightLimit.Total)
            {
                OwlLogger.LogError("Invalid Weight Modification attempted!", GameComponent.Items);
                RecalculateCharacterWeight(character);
                return;
            }

            character.CurrentWeight = newWeight;
            character.Connection.Send(new WeightPacket() { EntityId = character.Id, NewCurrentWeight = newWeight });
        }

        public void RecalculateCharacterWeight(CharacterRuntimeData character)
        {
            if(character == null)
            {
                OwlLogger.LogError("Character can't be null!", GameComponent.Items);
                return;
            }

            Inventory inv = GetOrLoadInventory(character.InventoryId);
            if(inv == null)
            {
                OwlLogger.LogError("Getting character inventory failed.", GameComponent.Items);
                return;
            }

            int totalWeight = 0;
            foreach(var kvp in inv.ItemStacksByTypeId)
            {
                totalWeight += kvp.Value.ItemType.Weight * kvp.Value.ItemCount;
            }

            if (totalWeight < 0 || totalWeight > character.WeightLimit.Total)
            {
                OwlLogger.LogError($"Weight recalculation for character {character.CharacterId} returned invalid value {totalWeight} - inventory corrupted!!", GameComponent.Items);
                return;
            }

            character.CurrentWeight = totalWeight;
            if(character.Connection.CharacterId != -1) // character login not yet completed for this character - we're currently creating this CharacterRuntimeData
            {
                character.Connection.Send(new WeightPacket() { EntityId = character.Id, NewCurrentWeight = totalWeight });
            }
        }
    }
}
