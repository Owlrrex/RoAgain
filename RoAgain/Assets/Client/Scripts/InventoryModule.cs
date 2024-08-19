using System.Collections;
using System.Collections.Generic;
using OwlLogging;
using Shared;

namespace Client
{
    public class Inventory
    {
        public int InventoryId;
        public Dictionary<long, ItemStack> ItemStacks;
    }

    public class ItemStack
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
        public HashSet<JobId> RequiredJobs;
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
                RequiredJobs = new(packet.RequiredJobs),
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
        private ServerConnection _connection;
        private Dictionary<long, ItemType> _knownItemTypes = new();
        private Inventory PlayerMainInventory;
        private Inventory PlayerCartInventory;
        private Inventory AccountStorageInventory;
        private Inventory GuildStorageInventory;
        private Dictionary<int, Inventory> GenericInventories;

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
        }

        private void OnItemStackReceived(int inventoryId, long itemTypeId, int count)
        {
            Inventory targetInventory = GetInventory(inventoryId);

            if (targetInventory == null)
            {
                OwlLogger.LogError($"Received ItemStack for InventoryId {inventoryId} that we have not received data for!", GameComponent.Items);
                // TODO: Should we allow blindly creating an inventory for this, assuming we'll receive an InventoryData-packet soon?
                return;
            }

            ItemType type = GetKnownItemType(itemTypeId);
            if(type == null)
            {
                OwlLogger.LogError($"Received ItemStack for ItemType {itemTypeId} that we have not received data for!", GameComponent.Items);
                // TODO: Implement "holding stack back until itemtype is recieved" system instead of logging error
                return;
            }

            targetInventory.ItemStacks.Add(itemTypeId, new() { ItemType = type, ItemCount = count });
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

            if (!targetInventory.ItemStacks.ContainsKey(itemTypeId))
                OwlLogger.LogError($"Received ItemStackRemoved for InventoryId {inventoryId} & ItemType {itemTypeId} that's not contained in local copy!", GameComponent.Items);
            targetInventory.ItemStacks.Remove(itemTypeId);
        }

        private void OnInventoryIdReceived(int inventoryId, int ownerEntityId)
        {
            switch (ownerEntityId)
            {
                case InventoryOwnerIds.ACCSTORAGE:
                    if (AccountStorageInventory == null)
                    {
                        AccountStorageInventory = new()
                        {
                            InventoryId = inventoryId
                        };
                    }
                    else
                    {
                        // Should rarely happen - the Server changed which InventoryId is our Account-Storage. In this case, we want to invalidate our local cache
                        AccountStorageInventory.InventoryId = inventoryId;
                        AccountStorageInventory.ItemStacks.Clear();
                    }
                    break;
                case InventoryOwnerIds.GUILDSTORAGE:
                    if (GuildStorageInventory == null)
                    {
                        GuildStorageInventory = new()
                        {
                            InventoryId = inventoryId
                        };
                    }
                    else
                    {
                        // Should rarely happen - the Server changed which InventoryId is our Guild-Storage. In this case, we want to invalidate our local cache
                        GuildStorageInventory.InventoryId = inventoryId;
                        GuildStorageInventory.ItemStacks.Clear();
                    }
                    break;
                case InventoryOwnerIds.PLAYERCART:
                    if (PlayerCartInventory == null)
                    {
                        PlayerCartInventory = new()
                        {
                            InventoryId = inventoryId
                        };
                    }
                    else
                    {
                        // Should rarely happen - the Server changed which InventoryId is our Cart. In this case, we want to invalidate our local cache
                        PlayerCartInventory.InventoryId = inventoryId;
                        PlayerCartInventory.ItemStacks.Clear();
                    }
                    break;
                default:
                    if (ownerEntityId == ClientMain.Instance.CurrentCharacterData.InventoryId)
                    {
                        if (PlayerMainInventory == null)
                        {
                            PlayerMainInventory = new()
                            {
                                InventoryId = inventoryId
                            };
                        }
                        else
                        {
                            // Should rarely happen - the Server changed which InventoryId is our MainInventory. In this case, we want to invalidate our local cache
                            PlayerMainInventory.InventoryId = inventoryId;
                            PlayerMainInventory.ItemStacks.Clear();
                        }
                    }
                    else
                    {
                        // generic Inventories - no precedence for this in RO, what could these be used for? Trades, Inspects?
                        // Best just keep them around
                        if (GenericInventories.ContainsKey(ownerEntityId))
                        {
                            // Inventory for a certain owner has changed - clear cache
                            GenericInventories[ownerEntityId].InventoryId = inventoryId;
                            GenericInventories[ownerEntityId].ItemStacks.Clear();
                        }
                        else
                        {
                            GenericInventories.Add(ownerEntityId, new()
                            {
                                InventoryId = inventoryId
                            });
                        }
                    }
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
            if (inventoryId == PlayerMainInventory.InventoryId)
                targetInventory = PlayerMainInventory;
            else if (inventoryId == PlayerCartInventory.InventoryId)
                targetInventory = PlayerCartInventory;
            else if (inventoryId == AccountStorageInventory.InventoryId)
                targetInventory = AccountStorageInventory;
            else if (inventoryId == GuildStorageInventory.InventoryId)
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

