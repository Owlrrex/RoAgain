namespace Shared
{
    /// <summary>
    /// Describes how an item can be used/interacted with by the player
    /// Also used by the Client to categorize inventory displays
    /// </summary>
    public enum ItemUsageMode
    {
        Unusable,
        Usable,
        Equip_Head_Upper,
        EQUIP_START = Equip_Head_Upper,
        Equip_head_Mid,
        Equip_Head_Lower,
        Equip_Armor,
        Equip_Garment,
        Equip_Shoes,
        Equip_Accessory_Any,
        Equip_Accessory_Left,
        Equip_Accessory_Right,
        Equip_Mainhand,
        Equip_Offhand,
        Equip_Twohand,
        EQUIP_END = Equip_Twohand
    }

    /// <summary>
    /// Represents a type of modifier applied to an ItemType
    /// </summary>
    public enum ModifierType
    {
        Unknown,
        CardSlot_1, // value = Card's ItemTypeId
        CardSlot_2, // value = Card's ItemTypeId
        CardSlot_3, // value = Card's ItemTypeId
        CardSlot_4, // value = Card's ItemTypeId
        CrafterName, // value = Crafter's CharacterId
        UpgradeAmount, // value = upgrade value
        OnEquipScript, // value = ScriptId
        Unidentified, // value irrelevant, should be > 0 though
        CraftingAdditive_1, // value == Additive's ItemTypeId
        CraftingAdditive_2, // value == Additive's ItemTypeId
        CraftingAdditive_3, // value == Additive's ItemTypeId
        OnInventoryEnterScript, // forgot what this was gonna be used for, but it's in the design so probably there was something
        OnInventoryExitScript, // forgot what this was gonna be used for, but it's in the design so probably there was something
    }

    public static class InventoryOwnerIds
    {
        public const int ACCSTORAGE = -1;
        public const int GUILDSTORAGE = -2;
        public const int PLAYERCART = -3; // This means inspecting other player's carts will not be possible, since we can't associate cart-inventories to entities!
    }

    public static class ItemConstants
    {
        public const long ITEM_TYPE_ID_INVALID = -1;
        public const long BASETYPEID_NONE = ITEM_TYPE_ID_INVALID;
    }
}
