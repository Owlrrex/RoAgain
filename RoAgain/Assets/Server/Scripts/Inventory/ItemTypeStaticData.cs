using OwlLogging;

namespace Server
{
    /// <summary>
    /// Persisted data for a single Item Type
    /// </summary>
    public class ItemTypeStaticData
    {

    }

    /// <summary>
    /// Persisted data for a single equippable item (including ammo)
    /// </summary>
    public class EquippableTypeStaticData : ItemTypeStaticData
    {

    }

    /// <summary>
    /// Persisted data for a single usable item (excluding equipment & ammo)
    /// </summary>
    public class UsableTypeStaticData : ItemTypeStaticData
    {

    }
}