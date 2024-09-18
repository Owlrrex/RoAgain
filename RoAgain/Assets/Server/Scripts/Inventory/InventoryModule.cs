using OwlLogging;
using Shared;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Provides functions to manipulate Inventories & the Items in them
    /// </summary>
    public class InventoryModule
    {
        private AInventoryDatabase _invDb;
        private ItemTypeModule _itemTypeModule;

        private Dictionary<int, Inventory> _cachedInventories = new();

        private Dictionary<int, HashSet<long>> _knownItemTypesByPlayerEntity = new();

        private Dictionary<int, CharacterRuntimeData> _characterInventories = new();

        public int Initialize(ItemTypeModule itemTypeModule, AInventoryDatabase invDb)
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
            foreach(var entry in persData.ItemsWrapper.entries)
            {
                inventory.ItemStacksByTypeId.Add(entry.key, CreateItemStack(entry.key, entry.value));
            }

            return inventory;
        }

        private InventoryPersistenceData InventoryToPersData(Inventory inv)
        {
            InventoryPersistenceData data = new()
            {
                InventoryId = inv.InventoryId,
                ItemsWrapper = new()
            };

            foreach (var kvp in inv.ItemStacksByTypeId)
            {
                data.ItemsWrapper.entries.Add(new()
                {
                    key = kvp.Key,
                    value = kvp.Value.ItemCount
                });
            }

            return data;
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

        private void DestroyItemStack(ItemStack stack)
        {
            _itemTypeModule.NotifyItemStackDestroyed(stack);
            AutoInitResourcePool<ItemStack>.Return(stack);
        }

        public int AddItemsToInventory(int inventoryId, long itemTypeId, int count)
        {
            if(itemTypeId <= 0)
            {
                OwlLogger.LogError("Can't add items with invalid itemTypeId", GameComponent.Items);
                return -2;
            }

            if (count <= 0)
            {
                OwlLogger.LogError($"Can't add ItemStack with ItemCount <=0 to Inventory {inventoryId}, ItemType {itemTypeId}", GameComponent.Items);
                return -3;
            }

            Inventory inventory = GetOrLoadInventory(inventoryId);

            if (inventory == null)
            {
                OwlLogger.LogError("Can't add items to null inventory!", GameComponent.Items);
                return -1;
            }

            if (inventory.HasItemTypeExact(itemTypeId))
            {
                inventory.ItemStacksByTypeId[itemTypeId].ItemCount += count;
            }
            else
            {
                ItemStack stack = CreateItemStack(itemTypeId, count);
                if (stack == null)
                {
                    OwlLogger.LogError("Creating ItemStack failed.", GameComponent.Items);
                    return -4;
                }

                inventory.ItemStacksByTypeId.Add(stack.ItemType.TypeId, stack);
            }

            NotifyInventoryListenerAddStack(itemTypeId, count, inventory);

            return 0;
        }

        public int AddItemsToCharacterInventory(CharacterRuntimeData character, long itemTypeId, int count)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't add items to null character!", GameComponent.Items);
                return -1;
            }

            if (!HasPlayerSpaceForItemStack(character, itemTypeId, count))
            {
                OwlLogger.LogF("Character {0} can't receive {1}x itemType {2} - not enough weight capacity.", character.CharacterId, count, itemTypeId, GameComponent.Items, LogSeverity.Verbose);
                return -2;
            }

            // If implementing ItemStack-limit: Check here

            int result = AddItemsToInventory(character.InventoryId, itemTypeId, count);

            return result;
        }

        public int RemoveItemsFromInventory(int inventoryId, long itemTypeId, int count, bool shouldDeleteStack)
        {
            if (itemTypeId <= 0)
            {
                OwlLogger.LogError("Can't remove items with invalid itemTypeId", GameComponent.Items);
                return -2;
            }

            if (count <= 0)
            {
                OwlLogger.LogError($"Can't remove ItemStack with ItemCount 0 from Inventory {inventoryId}, ItemType {itemTypeId}", GameComponent.Items);
                return -3;
            }

            Inventory inventory = GetOrLoadInventory(inventoryId);
            if (inventory == null)
            {
                OwlLogger.LogError("Can't remove items from null inventory!", GameComponent.Items);
                return -1;
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

            // TODO: Check for equipped-state here - unequip items or deny deletion, based on function arguments

            stack.ItemCount -= count;

            if(stack.ItemCount == 0)
            {
                int removeResult = RemoveItemStackFromInventory(inventory, itemTypeId);
                if (shouldDeleteStack)
                {
                    DestroyItemStack(stack);
                }
            }

            NotifyInventoryListenerRemoveStack(itemTypeId, count, inventory);

            return 0;
        }

        public int RemoveItemsFromCharacterInventory(CharacterRuntimeData character, long itemTypeId, int count, bool allowDeleteStack)
        {
            if (character == null)
            {
                OwlLogger.LogError("Can't remove items from null character!", GameComponent.Items);
                return -1;
            }

            return RemoveItemsFromInventory(character.InventoryId, itemTypeId, count, allowDeleteStack);
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

            // Can't attempt to delete ItemType from ItemTypeModule here - a Stack of this type may be added to another inventory yet
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

        public int SendItemStackDataToCharacter(CharacterRuntimeData character, long itemTypeId)
        {
            SendItemTypeDataToCharacterIfUnknown(character, itemTypeId);

            Inventory inventory = GetOrLoadInventory(character.InventoryId);
            int count = inventory.GetItemCountExact(itemTypeId);
            Packet packet;
            if (count == 0)
            {
                packet = new ItemStackRemovedPacket()
                {
                    InventoryId = inventory.InventoryId,
                    ItemTypeId = itemTypeId
                };
            }
            else
            {
                packet = new ItemStackPacket()
                {
                    InventoryId = character.InventoryId,
                    ItemTypeId = itemTypeId,
                    ItemCount = count
                };
            }    

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
            if (type.BaseTypeId != ItemConstants.BASETYPEID_NONE)
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

        public void PersistAllCachedInventories()
        {
            foreach(var kvp in _cachedInventories)
            {
                _invDb.Persist(InventoryToPersData(kvp.Value));
            }
        }

        public void HandleItemDropRequest(CharacterRuntimeData character, long itemTypeId, int inventoryId, int amount)
        {
            if(!CanCharacterAccessInventory(character, inventoryId))
            {
                OwlLogger.LogError($"Character {character.CharacterId} tried to drop items from inventory {inventoryId} that they can't access!", GameComponent.Items);
                return;
            }

            // TODO: Check ItemMoveRestrictions

            int removeResult = RemoveItemsFromInventory(inventoryId, itemTypeId, amount, false);

            if (removeResult != 0)
            {
                OwlLogger.LogError($"Failed to remove Items for ItemDropRequest", GameComponent.Items);
                return;
            }

            // TODO: Make PickupModule generate a pickup
        }

        public bool CanCharacterAccessInventory(CharacterRuntimeData character, int inventoryId)
        {
            if (character.InventoryId == inventoryId)
                return true; // Character inventory

            // TODO: Cart inventory
            // TODO: A storage that the character has currently opened
            return false;
        }

        public void RegisterCharacterInventory(CharacterRuntimeData character)
        {
            if (_characterInventories.ContainsKey(character.InventoryId))
            {
                OwlLogger.LogWarning($"Tried to register CharacterInventory for character {character.CharacterId} that was already registered!", GameComponent.Items);
                return;
            }

            _characterInventories[character.InventoryId] = character;
        }

        public void UnregisterCharacterInventory(CharacterRuntimeData character)
        {
            if (!_characterInventories.ContainsKey(character.InventoryId))
            {
                OwlLogger.LogWarning($"Tried to unregister CharacterInventory for character {character.CharacterId} that wasn't registered!", GameComponent.Items);
                return;
            }

            _characterInventories.Remove(character.InventoryId);
        }

        private void NotifyInventoryListenerAddStack(long addedTypeId, int addedCount, Inventory inventory)
        {
            if (!_characterInventories.ContainsKey(inventory.InventoryId))
                return;

            CharacterRuntimeData character = _characterInventories[inventory.InventoryId];
            SendItemStackDataToCharacter(character, addedTypeId);

            ItemType type = _itemTypeModule.GetOrLoadItemType(addedTypeId);
            if (type == null)
            {
                OwlLogger.LogError($"Fetching itemtype {addedTypeId} failed!", GameComponent.Items);
                return;
            }

            if (type.Weight > 0)
                ModifyCharacterWeight(character, type.Weight * addedCount);

            // TODO: Check other listening-modes (e.g. cart-owners) via separate lists
        }

        private void NotifyInventoryListenerRemoveStack(long removedTypeId, int removedCount, Inventory inventory)
        {
            if(!_characterInventories.ContainsKey(inventory.InventoryId))
                return;

            CharacterRuntimeData character = _characterInventories[inventory.InventoryId];
            SendItemStackDataToCharacter(character, removedTypeId);

            ItemType type = _itemTypeModule.GetOrLoadItemType(removedTypeId);
            if (type == null)
            {
                OwlLogger.LogError($"Fetching itemtype {removedTypeId} failed!", GameComponent.Items);
                return;
            }

            if (type.Weight > 0)
                ModifyCharacterWeight(character, -type.Weight * removedCount);

            // TODO: Check other listening-modes (e.g. cart-owners) via separate lists
        }
    }
}
