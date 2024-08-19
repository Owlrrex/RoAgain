using OwlLogging;

namespace Server
{
    /// <summary>
    /// Provides functions to manipulate what items characters have equipped.
    /// </summary>
    public class EquipmentModule
    {
        public int Initialize(InventoryModule invModule, ItemTypeModule itemTypeModule) // InvModule: To check item ownership ItemTypeModule: To resolve equipment effects
        {
            return 0;
        }

        public void Shutdown()
        {

        }
    }
}
