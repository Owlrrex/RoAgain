using OwlLogging;
using System;

namespace Shared
{
    [Flags]
    public enum EquipmentSlot
    {
        Unknown             = 0,
        HeadUpper           = 1 << 1,
        HeadMid             = 1 << 2,
        HeadLower           = 1 << 3,
        Armor               = 1 << 4,
        Garment             = 1 << 5,
        Shoes               = 1 << 6,
        Offhand             = 1 << 7,
        Mainhand            = 1 << 8,
        TwoHand             = 1 << 9,
        AccessoryLeft       = 1 << 10,
        AccessoryRight      = 1 << 11,
        Ammo                = 1 << 12,
        CostumeHeadUpper    = 1 << 13,
        CostumeHeadMid      = 1 << 14,
        CostumeHeadLower    = 1 << 15,
        CostumeBack         = 1 << 16,
    }

    public enum EquipmentType
    {
        Unknown,
        Dagger,
        OneHandSword,
        TwoHandSword,
        OneHandAxe,
        TwoHandAxe,
        OneHandSpear,
        TwoHandSpear,
        Mace,
        OneHandRod,
        TwoHandRod,
        Bow,
        Katar,
        Whip,
        Instrument,
        Knuckle,
    }
}
