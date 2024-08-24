using System.Collections.Generic;
using OwlLogging;
using Shared;

namespace Client
{
    public class Inventory
    {
        public int InventoryId;
        public Dictionary<long, ItemStack> ItemStacks = new();
    }

    public class ItemStack // TODO: Pool these to reduce allocations from pickups & other temporary ItemStacks
    {
        public ItemType ItemType;
        public int ItemCount;
    }

    public class ItemType
    {
        public const long BASETYPEID_NONE = -1; // Make sure to keep this in sync with Server's ItemType constant!

        public long TypeId;
        public long BaseTypeId;
        public int Weight;
        public int RequiredLevel;
        public JobFilter RequiredJobs;
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
                RequiredLevel = packet.RequiredLevel,
                RequiredJobs = packet.RequiredJobs,
                NumTotalCardSlots = packet.NumTotalCardSlots,
                UsageMode = packet.UsageMode,
                VisualId = packet.VisualId,
                NameLocId = packet.NameLocId,
                FlavorLocId = packet.FlavorLocId,
                Modifiers = packet.Modifiers.ToDict()
            };
        }
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

        private class PendingItemStackEntry
        {
            public int InventoryId;
            public long ItemTypeId;
            public int Count;
        }
        private Dictionary<long, List<PendingItemStackEntry>> _pendingItemStacks = new();

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
                foreach(PendingItemStackEntry entry in  _pendingItemStacks[newType.TypeId])
                {
                    OnItemStackReceived(entry.InventoryId, entry.ItemTypeId, entry.Count);
                }
                _pendingItemStacks.Remove(newType.TypeId);
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
                    if (GenericInventories.ContainsKey(i) == true)
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

            if(targetInventory.ItemStacks.ContainsKey(type.TypeId))
            {
                targetInventory.ItemStacks[type.TypeId].ItemCount += count;
            }
            else
            {
                targetInventory.ItemStacks.Add(itemTypeId, new() { ItemType = type, ItemCount = count });
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

            _pendingItemStacks[itemTypeId].Add(new() { InventoryId = inventoryId, ItemTypeId = itemTypeId, Count = count });
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
    }
}

