using OwlLogging;
using Shared;
using System;

namespace Server
{
    /// <summary>
    /// Represents a single stack of items in an Inventory. All Items are of the exact same type (including modifiers via DynamicItemTypes)
    /// </summary>
    [Serializable]
    public class ItemStack : IAutoInitPoolObject
    {
        public ItemType ItemType;
        public int ItemCount;

        public void Reset()
        {
            ItemType = null;
            ItemCount = 0;
        }

        public int GetStackWeight()
        {
            return ItemType.Weight * ItemCount;
        }
    }
}
