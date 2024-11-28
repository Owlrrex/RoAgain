using System;
using System.Collections.Generic;
using OwlLogging;
using Shared;

namespace Client
{
    public class Inventory
    {
        public int InventoryId;
        public Dictionary<long, ItemStack> ItemStacks = new();

        public Action<ItemStack> ItemStackAdded;
        public Action<ItemStack> ItemStackUpdated;
        public Action<long> ItemStackRemoved;
    }

    public class ItemStack // TODO: Pool these to reduce allocations from pickups & other temporary ItemStacks
    {
        public ItemType ItemType;
        public int ItemCount;
    }

    public class ItemType
    {
        public long TypeId;
        public long BaseTypeId;
        public int Weight;
        public int NumTotalCardSlots;
        public ItemUsageMode UsageMode;
        public int VisualId;
        public LocalizedStringId NameLocId;
        public LocalizedStringId FlavorLocId;
        public Dictionary<ModifierType, int> Modifiers;

        public static ItemType FromPacket(ItemTypePacket packet)
        {
            return new()
            {
                TypeId = packet.TypeId,
                BaseTypeId = packet.BaseTypeId,
                Weight = packet.Weight,
                NumTotalCardSlots = packet.NumTotalCardSlots,
                UsageMode = packet.UsageMode,
                VisualId = packet.VisualId,
                NameLocId = packet.NameLocId,
                FlavorLocId = packet.FlavorLocId,
                Modifiers = packet.Modifiers.ToDict()
            };
        }

        public bool MatchesFilter(InventoryFilter filter)
        {
            if (filter.HasFlag(InventoryFilter.Any))
                return true;

            if(filter.HasFlag(InventoryFilter.Consumable))
            {
                return UsageMode == ItemUsageMode.Consumable;
            }

            if(filter.HasFlag(InventoryFilter.Equippable))
            {
                return UsageMode == ItemUsageMode.Equip;
            }

            if(filter.HasFlag(InventoryFilter.Other))
            {
                return UsageMode == ItemUsageMode.Unusable;
            }

            return false;
        }
    }

    public class EquippableItemType : ItemType
    {
        public Dictionary<EquipmentSlot, List<IBattleEntityCriterium>> SlotCriteriums = new();

        public List<SimpleStatEntry> SimpleStats = new();
        public List<ConditionalStatEntry> ConditionalStats = new();

        public EquipmentType EquipmentType;

        public static EquippableItemType FromPacket(EquippableItemTypePacket packet)
        {
            EquippableItemType type = new()
            {
                TypeId = packet.TypeId,
                BaseTypeId = packet.BaseTypeId,
                Weight = packet.Weight,
                NumTotalCardSlots = packet.NumTotalCardSlots,
                UsageMode = packet.UsageMode,
                VisualId = packet.VisualId,
                NameLocId = packet.NameLocId,
                FlavorLocId = packet.FlavorLocId,
                Modifiers = packet.Modifiers.ToDict(),
                EquipmentType = packet.EquipmentType
            };
            foreach(var entry in packet.SlotCriteriums.entries)
            {
                type.SlotCriteriums.Add(entry.key, BecHelper.ParseCriteriumList(entry.value));
            }
            foreach(string statStr in packet.SimpleStatStrings)
            {
                type.SimpleStats.Add(SimpleStatEntry.FromString(statStr));
            }
            foreach(string statStr in packet.ConditionalStatStrings)
            {
                type.ConditionalStats.Add(ConditionalStatEntry.FromString(statStr, BecHelper.ConditionIdResolver));
            }
            return type;
        }
    }

    public class ConsumableItemType : ItemType
    {
        public List<IBattleEntityCriterium> UsageCriteriums = new();

        public static ConsumableItemType FromPacket(ConsumableItemTypePacket packet)
        {
            ConsumableItemType type = new()
            {
                TypeId = packet.TypeId,
                BaseTypeId = packet.BaseTypeId,
                Weight = packet.Weight,
                NumTotalCardSlots = packet.NumTotalCardSlots,
                UsageMode = packet.UsageMode,
                VisualId = packet.VisualId,
                NameLocId = packet.NameLocId,
                FlavorLocId = packet.FlavorLocId,
                Modifiers = packet.Modifiers.ToDict(),
                UsageCriteriums = BecHelper.ParseCriteriumList(packet.UsageCriteriumsList)
            };
            return type;
        }
    }

    public class EquipmentSet : EquipmentSet<EquippableItemType>
    {
        
    }

    [Flags]
    public enum InventoryFilter
    {
        Unknown = 0,
        Any = 1 << 0,
        Consumable = 1 << 1,
        Equippable = 1 << 2,
        Other = 1 << 3,
    }

    public class InventoryModule
    {
        public Inventory PlayerMainInventory;
        public Inventory PlayerCartInventory;
        public Inventory AccountStorageInventory;
        public Inventory GuildStorageInventory;
        public Dictionary<int, Inventory> GenericInventories = new();

        private ServerConnection _connection;
        private Dictionary<long, ItemType> _knownItemTypes = new();

        private Dictionary<int, EquipmentSet> _equipSets = new();

        private readonly LocalizedStringId _equipMsgLocId = new(223);
        private readonly LocalizedStringId _unequipMsgLocId = new(224);

        private class PendingItemStackEntry
        {
            public int InventoryId;
            public int Count;
        }
        private class PendingEquipmentSlotEntry
        {
            public int OwnerEntityId;
            public EquipmentSlot Slot;
        }
        private Dictionary<long, List<PendingItemStackEntry>> _pendingItemStacks = new();
        private Dictionary<long, List<PendingEquipmentSlotEntry>> _pendingEquipSlots = new();

        public int Initialize(ServerConnection connection)
        {
            if(connection == null)
            {
                OwlLogger.LogError("Can't initialize InventoryModule with null connection!", GameComponent.Items);
                return -1;
            }

            _connection = connection;
            _connection.InventoryReceived += OnInventoryIdReceived;
            _connection.ItemStackReceived += OnItemStackReceived;
            _connection.ItemStackRemovedReceived += OnItemStackRemovedReceived;
            _connection.ItemTypeReceived += OnItemTypeReceived;
            _connection.EquippableItemTypeReceived += OnItemTypeReceived; // Currently no special logic for Equippable types
            _connection.ConsumableItemTypeReceived -= OnItemTypeReceived; // Currently no special logic for Consumable types
            _connection.EquipmentSlotReceived += OnEquipmentSlotReceived;

            return 0;
        }

        public void Shutdown()
        {
            if (_connection != null)
            {
                _connection.InventoryReceived -= OnInventoryIdReceived;
                _connection.ItemStackReceived -= OnItemStackReceived;
                _connection.ItemStackRemovedReceived -= OnItemStackRemovedReceived;
                _connection.ItemTypeReceived -= OnItemTypeReceived;
                _connection.EquippableItemTypeReceived -= OnItemTypeReceived;
                _connection.ConsumableItemTypeReceived -= OnItemTypeReceived;
                _connection.EquipmentSlotReceived -= OnEquipmentSlotReceived;
            }
            _connection = null;
            PlayerMainInventory = null;
            PlayerCartInventory = null;
            AccountStorageInventory = null;
            GuildStorageInventory = null;
            GenericInventories.Clear();

            _knownItemTypes.Clear();
        }

        private void OnItemTypeReceived(ItemType newType)
        {
            if (newType == null)
            {
                OwlLogger.LogError("Received null ItemType!", GameComponent.Items);
                return;
            }

            _knownItemTypes[newType.TypeId] = newType;

            if (_pendingItemStacks.ContainsKey(newType.TypeId))
            {
                foreach(PendingItemStackEntry entry in _pendingItemStacks[newType.TypeId])
                {
                    OnItemStackReceived(entry.InventoryId, newType.TypeId, entry.Count);
                }
                _pendingItemStacks.Remove(newType.TypeId);
            }
            if (_pendingEquipSlots.ContainsKey(newType.TypeId))
            {
                foreach(PendingEquipmentSlotEntry entry in _pendingEquipSlots[newType.TypeId])
                {
                    OnEquipmentSlotReceived(entry.OwnerEntityId, entry.Slot, newType.TypeId);
                }
                _pendingEquipSlots.Remove(newType.TypeId);
            }
        }

        private void OnItemStackReceived(int inventoryId, long itemTypeId, int count)
        {
            Inventory targetInventory = GetInventory(inventoryId);

            if (targetInventory == null)
            {
                OwlLogger.Log($"Received ItemStack for InventoryId {inventoryId} that we have not received data for!", GameComponent.Items);
                // Generate a mock-ownerId to hold this (hopefuly temporary) inventory
                for(int i = int.MinValue; i < 0; i++)
                {
                    if (GenericInventories.ContainsKey(i))
                        continue;

                    OnInventoryIdReceived(inventoryId, i);
                    break;
                }
                OnItemStackReceived(inventoryId, itemTypeId, count);
                return;
            }

            ItemType type = GetKnownItemType(itemTypeId);
            if(type == null)
            {
                OwlLogger.Log($"Received ItemStack for ItemType {itemTypeId} that we have not received data for!", GameComponent.Items);
                RegisterPendingItemStack(inventoryId, itemTypeId, count);
                return;
            }

            if (targetInventory.ItemStacks.ContainsKey(type.TypeId))
            {
                targetInventory.ItemStacks[type.TypeId].ItemCount = count;
                targetInventory.ItemStackUpdated?.Invoke(targetInventory.ItemStacks[type.TypeId]);
            }
            else
            {
                ItemStack newStack = new() { ItemType = type, ItemCount = count };
                targetInventory.ItemStacks.Add(itemTypeId, newStack);
                targetInventory.ItemStackAdded?.Invoke(newStack);
            }
        }

        private void RegisterPendingItemStack(int inventoryId, long itemTypeId, int count)
        {
            if(!_pendingItemStacks.ContainsKey(itemTypeId))
            {
                _pendingItemStacks.Add(itemTypeId, new());
            }

            foreach(PendingItemStackEntry entry in _pendingItemStacks[itemTypeId])
            {
                if(entry.InventoryId == inventoryId)
                {
                    entry.Count = count;
                    return;
                }
            }

            _pendingItemStacks[itemTypeId].Add(new() { InventoryId = inventoryId, Count = count });
        }

        private void OnItemStackRemovedReceived(int inventoryId, long itemTypeId)
        {
            Inventory targetInventory = GetInventory(inventoryId);

            if (targetInventory == null)
            {
                OwlLogger.LogError($"Received ItemStackRemoved for InventoryId {inventoryId} that we have not received data for!", GameComponent.Items);
                // TODO: Should we allow blindly creating an inventory for this, assuming we'll receive an InventoryData-packet soon?
                return;
            }

            bool found = false;
            if(_pendingItemStacks.ContainsKey(itemTypeId))
            {
                foreach(var pendingStack in _pendingItemStacks[itemTypeId])
                {
                    if (pendingStack.InventoryId == inventoryId)
                    {
                        _pendingItemStacks[itemTypeId].Remove(pendingStack);
                        found = true;
                        break;
                    }
                }
            }

            if (targetInventory.ItemStacks.ContainsKey(itemTypeId))
            {
                targetInventory.ItemStacks.Remove(itemTypeId);
                found = true;
            }
            
            if(!found)
                OwlLogger.LogError($"Received ItemStackRemoved for InventoryId {inventoryId} & ItemType {itemTypeId} that's not contained in local copy!", GameComponent.Items);

            targetInventory.ItemStackRemoved?.Invoke(itemTypeId);
        }

        public void OnInventoryIdReceived(int inventoryId, int ownerEntityId)
        {
            Inventory matchingInventory = GetInventory(inventoryId);
            matchingInventory ??= new() { InventoryId = inventoryId };

            switch (ownerEntityId)
            {
                case InventoryOwnerIds.ACCSTORAGE:
                    AccountStorageInventory = matchingInventory;
                    break;
                case InventoryOwnerIds.GUILDSTORAGE:
                    GuildStorageInventory = matchingInventory;
                    break;
                case InventoryOwnerIds.PLAYERCART:
                    PlayerCartInventory = matchingInventory;
                    break;
                default:
                    if (ownerEntityId == ClientMain.Instance.CurrentCharacterData.Id)
                        PlayerMainInventory = matchingInventory;
                    else
                        GenericInventories[ownerEntityId] = matchingInventory;                        
                    break;
            }
        }

        private bool IsItemTypeKnown(long itemTypeId)
        {
            return _knownItemTypes.ContainsKey(itemTypeId);
        }

        public ItemType GetKnownItemType(long itemTypeId)
        {
            if (!IsItemTypeKnown(itemTypeId))
                return null;
            return _knownItemTypes[itemTypeId];
        }

        public Inventory GetInventory(int inventoryId)
        {
            Inventory targetInventory = null;
            if (inventoryId == PlayerMainInventory?.InventoryId)
                targetInventory = PlayerMainInventory;
            else if (inventoryId == PlayerCartInventory?.InventoryId)
                targetInventory = PlayerCartInventory;
            else if (inventoryId == AccountStorageInventory?.InventoryId)
                targetInventory = AccountStorageInventory;
            else if (inventoryId == GuildStorageInventory?.InventoryId)
                targetInventory = GuildStorageInventory;
            else
            {
                foreach (Inventory inv in GenericInventories.Values)
                {
                    if (inv.InventoryId == inventoryId)
                    {
                        targetInventory = inv;
                        break;
                    }
                }
            }
            return targetInventory;
        }

        private void OnEquipmentSlotReceived(int ownerEntityId, EquipmentSlot slot, long itemTypeId)
        {
            EquippableItemType type = null;
            if(itemTypeId != ItemConstants.ITEM_TYPE_ID_INVALID)
            {
                type = GetKnownItemType(itemTypeId) as EquippableItemType;
                if (type == null)
                {
                    RegisterPendingEquipmentSlot(ownerEntityId, slot, itemTypeId);
                    OwlLogger.LogWarning($"Received Equipmentslot containing itemtype {itemTypeId} that's not known yet, or not Equippable!", GameComponent.Items);
                    return;
                }
            }

            if(!_equipSets.ContainsKey(ownerEntityId))
            {
                if(itemTypeId == ItemConstants.ITEM_TYPE_ID_INVALID)
                {
                    OwlLogger.LogWarning($"Received EquipmentSlot update unequipping item for owner {ownerEntityId} that's not registered on client!", GameComponent.Items);
                }

                _equipSets.Add(ownerEntityId, new());
                if(ownerEntityId == ClientMain.Instance.CurrentCharacterData.Id)
                {
                    PlayerUI.Instance.EquipmentWindow.SetSet(_equipSets[ownerEntityId]);
                }
            }
            EquipmentSet modifiedSet = _equipSets[ownerEntityId];

            // Unequip current items, if any, in occupied slots
            // Don't use SetItemTypeOnGroup here, so we can send messages for each individual unequipped item
            Dictionary<EquipmentSlot, EquippableItemType> oldItemTypes = new();
            foreach (EquipmentSlot singleTargetSlot in new EquipmentSlotIterator(slot))
            {
                if (!modifiedSet.HasItemEquippedInSlot(singleTargetSlot))
                {
                    oldItemTypes.Add(singleTargetSlot, null);
                    continue;
                }

                EquippableItemType singleTargetType = modifiedSet.GetItemType(singleTargetSlot);
                EquipmentSlot groupedSlots = modifiedSet.GetGroupedSlots(singleTargetSlot);
                foreach(EquipmentSlot groupedSlot in new EquipmentSlotIterator(groupedSlots))
                {
                    oldItemTypes.Add(groupedSlot, singleTargetType);
                }
                modifiedSet.SetItemTypeOnGroup(singleTargetSlot, null);

                // Show chatmessage
                string format = LocalizedStringTable.GetStringById(_unequipMsgLocId);
                string fullTypeName = LocalizedStringTable.GetStringById(singleTargetType.NameLocId); // TODO: Get name with all modifiers
                string slotName = groupedSlots.ToHumanReadableString();
                string msg = string.Format(format, fullTypeName, slotName);
                ChatMessageData data = new()
                {
                    ChannelTag = DefaultChannelTags.EQUIPMENT,
                    Message = msg,
                };
                ClientMain.Instance.ChatModule.OnChatMessageReceived(data);
            }

            // Equip new itemtype, if any is given & available
            if(type != null)
            {
                modifiedSet.SetItemType(slot, type);
                string format = LocalizedStringTable.GetStringById(_equipMsgLocId);
                string fullTypeName = LocalizedStringTable.GetStringById(type.NameLocId); // TODO: Get name with all modifiers
                string slotName = slot.ToHumanReadableString();
                string msg = string.Format(format, fullTypeName, slotName);
                ChatMessageData data = new()
                {
                    ChannelTag = DefaultChannelTags.EQUIPMENT,
                    Message = msg,
                };
                ClientMain.Instance.ChatModule.OnChatMessageReceived(data);                
            }

            // Broadcast equip change to set
            foreach (var kvp in oldItemTypes)
            {
                modifiedSet.EquipmentChanged?.Invoke(kvp.Key, kvp.Value);
            }
        }

        private void RegisterPendingEquipmentSlot(int ownerEntityId, EquipmentSlot slot, long itemTypeId)
        {
            PendingEquipmentSlotEntry newEntry = new() { OwnerEntityId = ownerEntityId, Slot = slot };
            if (!_pendingEquipSlots.ContainsKey(itemTypeId))
                _pendingEquipSlots.Add(itemTypeId, new());

            _pendingEquipSlots[itemTypeId].Add(newEntry);
        }
    }
}

