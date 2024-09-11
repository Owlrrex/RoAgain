using Shared;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// A conceptual container for ItemStacks. Referenced by Characters (personal inventory, cart), Accounts & Guilds (storage), and possibly more
    /// </summary>
    public class Inventory : IAutoInitPoolObject
    {
        public int InventoryId;
        public Dictionary<long, ItemStack> ItemStacksByTypeId = new();

        public int GetItemCountExact(long itemTypeId)
        {
            return ItemStacksByTypeId.ContainsKey(itemTypeId) ? ItemStacksByTypeId[itemTypeId].ItemCount : 0;
        }

        public List<ItemStack> GetItemStacksByBaseType(long baseTypeId)
        {
            List<ItemStack> results = new();
            foreach(var stack in ItemStacksByTypeId.Values)
            {
                if(stack.ItemType.BaseTypeId == baseTypeId)
                    results.Add(stack);
            }
            return results;
        }

        public bool HasItemTypeExact(long itemTypeId)
        {
            return ItemStacksByTypeId.ContainsKey(itemTypeId);
        }

        public void Reset()
        {
            InventoryId = 0;
            ItemStacksByTypeId.Clear();
        }
    }
}
