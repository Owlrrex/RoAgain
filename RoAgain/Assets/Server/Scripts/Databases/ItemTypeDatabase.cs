using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    /// <summary>
    /// Handles Loading of persisted ItemType data
    /// </summary>
    public abstract class AItemTypeDatabase
    {
        protected long _nextItemTypeId = 1;

        public abstract int Initialize(string config);

        public abstract void Shutdown();

        public abstract bool DoesItemTypeExist(long itemTypeId);

        public abstract long GetMatchingItemTypeIdExact(long baseTypeId, Dictionary<ModifierType, int> modifiers);

        public ItemType LoadItemType(long itemTypeId)
        {
            ItemTypePersistentData persData = LoadItemTypePersData(itemTypeId);
            if (persData == null)
                return null;

            if (!persData.IsValid())
            {
                OwlLogger.LogError($"ItemType {itemTypeId} can't be loaded - persistent data is invalid.", GameComponent.Persistence);
                return null;
            }

            ItemType type = PersDataToItemType(persData);
            if(!type.IsValid())
            {
                OwlLogger.LogError($"ItemType {itemTypeId} is invalid after conversion from PersistentData!", GameComponent.Persistence);
                return null;
            }

            return type;
        }

        protected abstract ItemTypePersistentData LoadItemTypePersData(long itemTypeId);

        public abstract ItemType CreateItemType(long baseTypeId, Dictionary<ModifierType, int> modifiers);

        public abstract int DeleteItemType(long itemTypeId);

        /// <summary>
        /// Clears out any unused ItemTypes from the database
        /// </summary>
        /// <param name="usedItemTypeIds">ItemTypes that are still in use by any persisted ItemStack</param>
        /// <returns>Error code</returns>
        public abstract int SanitizeItemTypeDatabase(IEnumerable<long> usedItemTypeIds);

        /// <summary>
        /// Clears out any unused ItemTypes from the database, and then reassigns ItemTypeIds so that the still-used types aren't unnecessarily sparse
        /// </summary>
        /// <param name="usedItemTypeIds">ItemTypes that are still in use by any persisted ItemStack</param>
        /// <returns>A mapping of which ItemTypeIds were reassigned as pairs of Key = Old, Value = New. Unchanged Ids may be absent or have an entry where Key = value. Null if failed.</returns>
        public abstract Dictionary<long, long> RebuildItemTypeDatabase(IEnumerable<long> usedItemTypeIds);

        protected ItemType PersDataToItemType(ItemTypePersistentData persData)
        {
            if(persData == null)
            {
                OwlLogger.LogError("Can't convert null ItemTypePersistenceData into ItemType", GameComponent.Items);
                return null;
            }

            if(persData.TypeId <= 0)
            {
                OwlLogger.LogError($"Can't convert ItemPersistenceData into ItemType - invalid TypeId {persData.TypeId}", GameComponent.Items);
                return null;
            }

            if(persData.BaseTypeId <= 0 && persData.BaseTypeId != ItemConstants.BASETYPEID_NONE)
            {
                OwlLogger.LogError($"Can't convert ItemPersistenceData {persData.TypeId} into ItemType - invalid BaseTypeId {persData.BaseTypeId}", GameComponent.Items);
                return null;
            }

            if (persData.Weight < 0)
            {
                OwlLogger.LogError($"Can't convert ItemPersistenceData {persData.TypeId} to ItemType - invalid Weight {persData.Weight}", GameComponent.Items);
                return null;
            }

            if (persData.SellPrice < 0)
            {
                OwlLogger.LogError($"Can't convert ItemPersistenceData {persData.TypeId} to ItemType - invalid SellPrice {persData.SellPrice}", GameComponent.Items);
                return null;
            }

            ItemType type;
            if (persData is EquippableTypePersistentData equipPersData)
            {
                EquippableItemType equipType = AutoInitResourcePool<EquippableItemType>.Acquire();
                equipType.EquipScript = equipPersData.EquipScriptId;
                equipType.UnequipScript = equipPersData.UnquipScriptId;
                equipType.EquipmentType = equipPersData.EquipmentType;
                foreach (var entry in equipPersData.SlotCriteriumStringLists.entries)
                {
                    equipType.SlotCriteriums.Add(entry.key, BECHelper.ParseCriteriumList(entry.value));
                }

                foreach(string str in equipPersData.SimpleStatStrings)
                {
                    equipType.SimpleStatEntries.Add(SimpleStatEntry.FromString(str));
                }

                foreach(string str in equipPersData.ConditionalStatStrings)
                {
                    equipType.ConditionalStatEntries.Add(ConditionalStatEntry.FromString(str, ConditionalStatHelpers.ConditionIdResolver));
                }

                type = equipType;
            }
            else if (persData is ConsumableTypePersistentData usablePersData)
            {
                ConsumableItemType consumeType = AutoInitResourcePool<ConsumableItemType>.Acquire(); // tmp while UsableItemType class doesn't exist yet
                consumeType.UseScriptId = usablePersData.UseScriptId;
                consumeType.UsageCriteriums = BECHelper.ParseCriteriumList(usablePersData.UseCriteriumStringList);
                type = consumeType;
            }
            else
            {
                type = AutoInitResourcePool<ItemType>.Acquire();
            }

            type.TypeId = persData.TypeId;
            type.BaseTypeId = persData.BaseTypeId;
            type.CanStack = persData.CanStack;
            type.Weight = persData.Weight;
            type.SellPrice = persData.SellPrice;
            type.NumTotalCardSlots = persData.NumTotalCardSlots;
            type.UsageMode = persData.UsageMode;
            
            type.NameLocId = persData.NameLocId;
            type.FlavorLocId = persData.FlavorLocId;
            type.VisualId = persData.VisualId;
            foreach(var entry in persData.Modifiers.entries)
            {
                if(type.HasModifier(entry.key))
                {
                    OwlLogger.LogError($"Can't convert ItemPersistenceData {persData.TypeId} into ItemType - duplicate modifier {entry.key}!", GameComponent.Items);
                    return null;
                }

                type.AddModifier(entry.key, entry.value);
            }

            return type;
        }
    }

    /// <summary>
    /// File-based implementation of AItemTypeDatabase
    /// </summary>
    public class ItemTypeDatabase : AItemTypeDatabase
    {
        // Note: DynamicItemTypes can be orphaned by operations that don't require items of that type & the type itself to be loaded, like character-deletion.
        // For this reason, an operation should be available that checks all currently persisted inventories and deletes any ItemTypes that are no longer needed (and ideally also flags other issues).
        // This operation would be meant to be run during maintenances, not while the server's running, since it's probably slow & would lock access to Inventories

        /// <summary>
        /// Maps ids of all ItemTypes currently in the Db to their base-type for faster search
        /// </summary>
        private Dictionary<long, List<long>> _itemTypeIdsByBaseType = new();

        private const string FILE_SUFFIX = ".itdb";
        private string _folderPath;

        public override int Initialize(string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                OwlLogger.LogError($"Can't initialize ItemTypeDatabase with empty config", GameComponent.Persistence);
                return -1;
            }

            if (!Directory.Exists(config))
            {
                OwlLogger.Log($"Creating ItemType database folder: {config}", GameComponent.Persistence);
                try
                {
                    Directory.CreateDirectory(config);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while creating ItemType Database folder at {config}: {e.Message}", GameComponent.Persistence);
                    return -2;
                }
            }

            _folderPath = config;

            /*
            // tmp: Create some ItemTypes
            List<ItemTypePersistentData> creationList = new();
            creationList.Add(new()
            {
                BaseTypeId = ItemConstants.BASETYPEID_NONE,
                CanStack = true,
                FlavorLocId = new() { Id = 208 },
                Modifiers = null,
                NameLocId = new() { Id = 207 },
                NumTotalCardSlots = 0,
                SellPrice = 10,
                TypeId = 1,
                UsageMode = ItemUsageMode.Unusable,
                Weight = 1,
                VisualId = 1
            });
            creationList.Add(new()
            {
                BaseTypeId = ItemConstants.BASETYPEID_NONE,
                CanStack = true,
                FlavorLocId = new() { Id = 210 },
                Modifiers = null,
                NameLocId = new() { Id = 209 },
                NumTotalCardSlots = 0,
                SellPrice = 10000,
                TypeId = 2,
                UsageMode = ItemUsageMode.Unusable,
                Weight = 100,
                VisualId = 2
            });
            creationList.Add(new ConsumableTypePersistentData()
            {
                BaseTypeId = ItemConstants.BASETYPEID_NONE,
                CanStack = true,
                FlavorLocId = new() { Id = 212 },
                Modifiers = null,
                NameLocId = new() { Id = 211 },
                NumTotalCardSlots = 1,
                UseScriptId = 0,
                SellPrice = 1,
                TypeId = 3,
                UsageMode = ItemUsageMode.Consumable,
                Weight = 1000,
                VisualId = 3
            });
            EquippableTypePersistentData equip1 = new EquippableTypePersistentData()
            {
                BaseTypeId = ItemConstants.BASETYPEID_NONE,
                CanStack = true,
                FlavorLocId = new() { Id = 226 },
                Modifiers = null,
                NameLocId = new() { Id = 225 },
                NumTotalCardSlots = 1,
                SellPrice = 1,
                TypeId = 4,
                UsageMode = ItemUsageMode.Equip,
                Weight = 20,
                EquipScriptId = 0,
                UnquipScriptId = 0,
                SimpleStatStrings = new(),
                ConditionalStatStrings = new(),
                SlotCriteriumStringLists = new(),
                VisualId = 4,
                EquipmentType = EquipmentType.Mace
            };
            Stat equip1Stat1 = new();
            equip1Stat1.ModifyAdd(15);
            equip1.SimpleStatStrings.Add($"({(int)EntityPropertyType.CurrentAtkBoth},{JsonUtility.ToJson(equip1Stat1, false)})");
            equip1.SlotCriteriumStringLists.entries.Add(new()
            {
                key = EquipmentSlot.Mainhand,
                value = "()"
            });
            creationList.Add(equip1);

            EquippableTypePersistentData equip2 = new EquippableTypePersistentData()
            {
                BaseTypeId = ItemConstants.BASETYPEID_NONE,
                CanStack = true,
                FlavorLocId = new() { Id = 228 },
                Modifiers = null,
                NameLocId = new() { Id = 227 },
                NumTotalCardSlots = 1,
                SellPrice = 100,
                TypeId = 5,
                UsageMode = ItemUsageMode.Equip,
                Weight = 50,
                EquipScriptId = 0,
                UnquipScriptId = 0,
                SimpleStatStrings = new(),
                ConditionalStatStrings = new(),
                SlotCriteriumStringLists = new(),
                VisualId = 5,
                EquipmentType = EquipmentType.Sword,
            };
            Stat equip2Stat1 = new();
            equip2Stat1.ModifyAdd(50);
            equip2.SimpleStatStrings.Add($"({(int)EntityPropertyType.CurrentAtkBoth},{JsonUtility.ToJson(equip2Stat1, false)})");
            Stat equip2Stat2 = new();
            equip2Stat2.ModifyAdd(10);
            equip2.SimpleStatStrings.Add($"({(int)EntityPropertyType.HardDef},{JsonUtility.ToJson(equip2Stat2, false)})");
            ConditionalStatEntry equip2Entry1 = new();
            equip2Entry1.Type = EntityPropertyType.MatkBoth;
            equip2Entry1.ConditionalChange = new() { Condition = new BelowHpThresholdPercentCondition() { Percentage = 0.25f } };
            equip2Entry1.ConditionalChange.Value.ModifyMult(0.5f); // 50% more matk when under 25% life
            equip2.ConditionalStatStrings.Add(equip2Entry1.Serialize());
            equip2.SlotCriteriumStringLists.entries.Add(new()
            {
                key = EquipmentSlot.TwoHand,
                value = "(1,20)" // require baselevel 20
            });
            creationList.Add(equip2);

            foreach (ItemTypePersistentData type in creationList)
            {
                string path = MakeFilePathForType(type.TypeId);
                string json = JsonUtility.ToJson(type, true);
                File.WriteAllText(path, json);
            }
            // end tmp

            OwlLogger.Log("Scanning ItemTypeDatabase...", GameComponent.Persistence);
            string[] typeFiles;
            int typeCount = 0;
            try
            {
                typeFiles = Directory.GetFiles(_folderPath, "*" + FILE_SUFFIX, SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while accessing files in ItemType Database: {e.Message}", GameComponent.Persistence);
                return -3;
            }

            foreach(string file in typeFiles)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if(!long.TryParse(filename, out long value))
                {
                    OwlLogger.LogError($"ItemType file {file} doesn't have parseable filename!", GameComponent.Persistence);
                    continue;
                }
                _nextItemTypeId = Math.Max(_nextItemTypeId, value+1);

                ItemTypePersistentData persData = LoadItemTypePersData(value);

                if(persData.BaseTypeId != ItemConstants.BASETYPEID_NONE)
                {
                    if (!_itemTypeIdsByBaseType.ContainsKey(persData.BaseTypeId))
                        _itemTypeIdsByBaseType[persData.BaseTypeId] = new();
                    _itemTypeIdsByBaseType[persData.BaseTypeId].Add(persData.TypeId);
                }

                typeCount++;
            }
            OwlLogger.LogF("Scanned {0} ItemTypes, next itemTypeId is {1}", typeCount, _nextItemTypeId, GameComponent.Persistence);

            // tmp: See if loading works.
            ItemType tmpType = LoadItemType(5);
            Packet packet = tmpType.ToPacket();
            string test = System.Text.Encoding.UTF8.GetString(packet.SerializeJson());
            // end tmp
            */

            return 0;
        }

        public override void Shutdown()
        {
            _itemTypeIdsByBaseType.Clear();
            _folderPath = null;
        }

        /// <summary>
        /// Finds an ItemType that matches the given BaseType & modifiers
        /// This operation may be very slow, even with the metadata that the Database gathers on startup.
        /// </summary>
        /// <param name="baseTypeId">Desired base-ItemTypeId</param>
        /// <param name="modifiers">Desired modifiers & values</param>
        /// <returns>The ItemTypeId of the matching type, or ItemConstants.ITEM_TYPE_ID_INVALID if none was found</returns>
        public override long GetMatchingItemTypeIdExact(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            if (!_itemTypeIdsByBaseType.ContainsKey(baseTypeId))
                return ItemConstants.ITEM_TYPE_ID_INVALID;

            List<long> canddiateIds = _itemTypeIdsByBaseType[baseTypeId];
            foreach (long id in canddiateIds)
            {
                if (DoModifiersMatch(id, modifiers))
                    return id;
            }

            return ItemConstants.ITEM_TYPE_ID_INVALID;
        }

        private bool DoModifiersMatch(long itemTypeId, Dictionary<ModifierType, int> targetModifiers)
        {
            ItemTypePersistentData persData = LoadItemTypePersData(itemTypeId);
            if (persData == null)
                return false;

            return persData.ModifiersMatchExact(targetModifiers);
        }

        protected override ItemTypePersistentData LoadItemTypePersData(long itemTypeId)
        {
            if(!DoesItemTypeExist(itemTypeId))
            {
                OwlLogger.LogError($"Can't load itemtype data for id {itemTypeId} - file not found", GameComponent.Persistence);
                return null;
            }

            string filePath = MakeFilePathForType(itemTypeId);

            string rawData;
            try
            {
                rawData = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Failed to load itemtype file {filePath}: {e.Message}", GameComponent.Persistence);
                return null;
            }

            ItemTypePersistentData persData;
            // TODO: Use "UsageMode" here to distinguish between Equippable, Consumable, Unusable?
            if (rawData.Contains("EquipScript")) // Provisional criterium to detect equippable types
            {
                persData = JsonUtility.FromJson<EquippableTypePersistentData>(rawData);
            }
            else if(rawData.Contains("UseScript")) // provisional criterium to detect usable types
            {
                persData = JsonUtility.FromJson<ConsumableTypePersistentData>(rawData);
            }
            else
            {
                persData = JsonUtility.FromJson<ItemTypePersistentData>(rawData);
            }
            
            if(!persData.IsValid())
            {
                OwlLogger.LogError($"ItemType {persData.TypeId} is invalid after JsonParse.", GameComponent.Persistence);
                return null;
            }

            return persData;
        }

        public override ItemType CreateItemType(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            // Is this alright to check? It may be slow.
            if(GetMatchingItemTypeIdExact(baseTypeId, modifiers) != ItemConstants.ITEM_TYPE_ID_INVALID)
            {
                OwlLogger.LogError($"Can't create itemType for baseItemType {baseTypeId} - type already exists!", GameComponent.Persistence);
                return null;
            }

            if(!DoesItemTypeExist(baseTypeId))
            {
                OwlLogger.LogError($"Can't create itemType for baseItemType {baseTypeId} - baseType doesn't exist!", GameComponent.Persistence);
                return null;
            }

            ItemTypePersistentData baseData = LoadItemTypePersData(baseTypeId);

            // TODO: Allow customizing this type in some ways, with different values from its base
            baseData.BaseTypeId = baseTypeId;
            baseData.Modifiers.FromDict(modifiers);
            baseData.TypeId = _nextItemTypeId++;

            string filePath = MakeFilePathForType(baseData.TypeId);
            try
            {
                string json = JsonUtility.ToJson(baseData);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while creating file for new ItemType {baseData.TypeId}: {e.Message}", GameComponent.Persistence);
                return null;
            }

            if (!_itemTypeIdsByBaseType.ContainsKey(baseTypeId))
                _itemTypeIdsByBaseType[baseTypeId] = new();
            _itemTypeIdsByBaseType[baseTypeId].Add(baseData.TypeId);

            return PersDataToItemType(baseData);
        }

        public override int DeleteItemType(long itemTypeId)
        {
            if (!DoesItemTypeExist(itemTypeId))
            {
                OwlLogger.LogError($"Can't delete itemtype {itemTypeId} - itemtype doesn't exist!", GameComponent.Persistence);
                return -1;
            }

            ItemTypePersistentData persData = LoadItemTypePersData(itemTypeId);
            // if this type exists, but isn't in the mapping, it's a broken/illegal type being used and we're probably ok to error out here.
            _itemTypeIdsByBaseType[persData.BaseTypeId].Remove(persData.TypeId);
            if (_itemTypeIdsByBaseType[persData.BaseTypeId].Count == 0)
                _itemTypeIdsByBaseType.Remove(persData.BaseTypeId);

            File.Delete(MakeFilePathForType(itemTypeId));
            return 0;
        }

        public override int SanitizeItemTypeDatabase(IEnumerable<long> usedItemTypeIds)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<long, long> RebuildItemTypeDatabase(IEnumerable<long> usedItemTypeIds)
        {
            SanitizeItemTypeDatabase(usedItemTypeIds);

            throw new NotImplementedException();
        }

        public override bool DoesItemTypeExist(long itemTypeId)
        {
            if (itemTypeId <= 0)
                return false;

            return File.Exists(MakeFilePathForType(itemTypeId));
        }

        private string MakeFilePathForType(long itemTypeId)
        {
            return Path.Combine(_folderPath, $"{itemTypeId}{FILE_SUFFIX}");
        }
    }
}
