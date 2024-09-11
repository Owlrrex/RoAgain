using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class ItemTypePersistentData
    {
        public long TypeId;
        public long BaseTypeId;
        public bool CanStack;
        public int Weight;
        public int SellPrice;
        public int RequiredLevel;
        public JobFilter RequiredJobs;
        public int NumTotalCardSlots;
        public ItemUsageMode UsageMode;
        public int OnUseScriptId;
        public LocalizedStringId NameLocId;
        public LocalizedStringId FlavorLocId;
        public int VisualId;
        public DictionarySerializationWrapper<ModifierType, int> Modifiers;

        public bool IsValid()
        {
            return TypeId >= 0
                && (BaseTypeId >= 0 || BaseTypeId == ItemType.BASETYPEID_NONE)
                && Weight >= 0
                && SellPrice >= 0;
        }

        public bool ModifiersMatchExact(Dictionary<ModifierType, int> targetModifiers)
        {
            bool ownModsEmpty = Modifiers == null || Modifiers.entries == null || Modifiers.entries.Count == 0;
            bool targetModsEmpty = targetModifiers == null || targetModifiers.Count == 0;
            if (ownModsEmpty != targetModsEmpty)
                return false;

            if (ownModsEmpty && targetModsEmpty) // Ensure both lists aren't empty for code below
                return true;

            if (targetModifiers.Count != Modifiers.entries.Count)
                return false;

            foreach (var entry in Modifiers.entries)
            {
                if (!targetModifiers.ContainsKey(entry.key))
                    return false;

                if (targetModifiers[entry.key] != entry.value)
                    return false;
            }

            return true;
        }
    }

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

            if(persData.BaseTypeId <= 0 && persData.BaseTypeId != ItemType.BASETYPEID_NONE)
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

            ItemType type = AutoInitResourcePool<ItemType>.Acquire();
            type.TypeId = persData.TypeId;
            type.BaseTypeId = persData.BaseTypeId;
            type.CanStack = persData.CanStack;
            type.Weight = persData.Weight;
            type.SellPrice = persData.SellPrice;
            type.RequiredLevel = persData.RequiredLevel;
            type.RequiredJobs = persData.RequiredJobs;
            type.NumTotalCardSlots = persData.NumTotalCardSlots;
            type.UsageMode = persData.UsageMode;
            type.OnUseScript = persData.OnUseScriptId;
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

        protected ItemTypePersistentData ItemTypeToPersData(ItemType type)
        {
            if (type == null)
            {
                OwlLogger.LogError("Can't convert null ItemType into PersistenceData", GameComponent.Items);
                return null;
            }

            if (type.TypeId <= 0)
            {
                OwlLogger.LogError($"Can't convert ItemType into PersistenceData - invalid TypeId {type.TypeId}", GameComponent.Items);
                return null;
            }

            if (type.BaseTypeId <= 0 && type.BaseTypeId != ItemType.BASETYPEID_NONE)
            {
                OwlLogger.LogError($"Can't convert ItemType {type.TypeId} into PersistenceData - invalid BaseTypeId {type.BaseTypeId}", GameComponent.Items);
                return null;
            }

            if (type.Weight < 0)
            {
                OwlLogger.LogError($"Can't convert ItemType {type.TypeId} to PersistenceData - invalid Weight {type.Weight}", GameComponent.Items);
                return null;
            }

            if (type.SellPrice < 0)
            {
                OwlLogger.LogError($"Can't convert ItemType {type.TypeId} to PersistenceData - invalid SellPrice {type.SellPrice}", GameComponent.Items);
                return null;
            }

            ItemTypePersistentData persData = new();
            persData.TypeId = type.TypeId;
            persData.BaseTypeId = type.BaseTypeId;
            persData.CanStack = type.CanStack;
            persData.Weight = type.Weight;
            persData.SellPrice = type.SellPrice;
            persData.RequiredLevel = type.RequiredLevel;
            persData.RequiredJobs = type.RequiredJobs;
            persData.NumTotalCardSlots = type.NumTotalCardSlots;
            persData.UsageMode = type.UsageMode;
            persData.OnUseScriptId = type.OnUseScript;
            persData.NameLocId = type.NameLocId;
            persData.FlavorLocId = type.FlavorLocId;
            persData.VisualId = type.VisualId;
            persData.Modifiers = new();
            persData.Modifiers.FromDict(type.ReadOnlyModifiers);

            return persData;
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

        public const long ITEM_TYPE_ID_INVALID = -1;

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
                _nextItemTypeId = Math.Max(_nextItemTypeId, value);

                ItemTypePersistentData persData = LoadItemTypePersData(value);

                if(persData.BaseTypeId != ItemType.BASETYPEID_NONE)
                {
                    if (!_itemTypeIdsByBaseType.ContainsKey(persData.BaseTypeId))
                        _itemTypeIdsByBaseType[persData.BaseTypeId] = new();
                    _itemTypeIdsByBaseType[persData.BaseTypeId].Add(persData.TypeId);
                }

                typeCount++;
            }
            OwlLogger.LogF("Scanned {0} ItemTypes, next itemTypeId is {1}", typeCount, _nextItemTypeId, GameComponent.Persistence);

            //// tmp: Create some ItemTypes
            //List<ItemTypePersistentData> creationList = new();
            //creationList.Add(new()
            //{
            //    BaseTypeId = ItemType.BASETYPEID_NONE,
            //    CanStack = true,
            //    FlavorLocId = new() { Id = 2001 },
            //    Modifiers = null,
            //    NameLocId = new() { Id = 2000 },
            //    NumTotalCardSlots = 0,
            //    OnUseScriptId = 0,
            //    RequiredJobs = JobFilter.Any,
            //    RequiredLevel = 0,
            //    SellPrice = 10,
            //    TypeId = 1,
            //    UsageMode = ItemUsageMode.Unusable,
            //    Weight = 1
            //});
            //creationList.Add(new()
            //{
            //    BaseTypeId = ItemType.BASETYPEID_NONE,
            //    CanStack = true,
            //    FlavorLocId = new() { Id = 2003 },
            //    Modifiers = null,
            //    NameLocId = new() { Id = 2002 },
            //    NumTotalCardSlots = 0,
            //    OnUseScriptId = 0,
            //    RequiredJobs = JobFilter.Any,
            //    RequiredLevel = 0,
            //    SellPrice = 10000,
            //    TypeId = 2,
            //    UsageMode = ItemUsageMode.Unusable,
            //    Weight = 100
            //});
            //creationList.Add(new()
            //{
            //    BaseTypeId = ItemType.BASETYPEID_NONE,
            //    CanStack = true,
            //    FlavorLocId = new() { Id = 2005 },
            //    Modifiers = null,
            //    NameLocId = new() { Id = 2004 },
            //    NumTotalCardSlots = 1,
            //    OnUseScriptId = 0,
            //    RequiredJobs = JobFilter.Any,
            //    RequiredLevel = 20,
            //    SellPrice = 1,
            //    TypeId = 3,
            //    UsageMode = ItemUsageMode.Usable,
            //    Weight = 1000
            //});

            //foreach (ItemTypePersistentData type in creationList)
            //{
            //    string path = MakeFilePathForType(type.TypeId);
            //    string json = JsonUtility.ToJson(type);
            //    File.WriteAllText(path, json);
            //}

            return 0;
        }

        public override void Shutdown()
        {
            _itemTypeIdsByBaseType.Clear();
        }

        public override long GetMatchingItemTypeIdExact(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            if (!_itemTypeIdsByBaseType.ContainsKey(baseTypeId))
                return ITEM_TYPE_ID_INVALID;

            List<long> canddiateIds = _itemTypeIdsByBaseType[baseTypeId];
            foreach (long id in canddiateIds)
            {
                if (DoModifiersMatch(id, modifiers))
                    return id;
            }

            return ITEM_TYPE_ID_INVALID;
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

            ItemTypePersistentData persData = JsonUtility.FromJson<ItemTypePersistentData>(rawData);
            if(!persData.IsValid())
            {
                OwlLogger.LogError($"ItemType {persData.TypeId} is invalid after JsonParse.", GameComponent.Persistence);
                return null;
            }

            return persData;
        }

        public override ItemType CreateItemType(long baseTypeId, Dictionary<ModifierType, int> modifiers)
        {
            if(GetMatchingItemTypeIdExact(baseTypeId, modifiers) != ITEM_TYPE_ID_INVALID)
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
            ItemTypePersistentData persdata = new();
            persdata.BaseTypeId = baseTypeId;
            persdata.CanStack = baseData.CanStack;
            persdata.Modifiers = new();
            persdata.Modifiers.FromDict(modifiers);
            persdata.NumTotalCardSlots = baseData.NumTotalCardSlots;
            persdata.OnUseScriptId = baseData.OnUseScriptId;
            persdata.RequiredJobs = baseData.RequiredJobs;
            persdata.RequiredLevel = baseData.RequiredLevel;
            persdata.SellPrice = baseData.SellPrice;
            persdata.TypeId = _nextItemTypeId++;
            persdata.UsageMode = baseData.UsageMode;
            persdata.Weight = baseData.Weight;

            string filePath = MakeFilePathForType(persdata.TypeId);
            try
            {
                string json = JsonUtility.ToJson(persdata);
                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while creating file for new ItemType {persdata.TypeId}: {e.Message}", GameComponent.Persistence);
                return null;
            }

            if (!_itemTypeIdsByBaseType.ContainsKey(baseTypeId))
                _itemTypeIdsByBaseType[baseTypeId] = new();
            _itemTypeIdsByBaseType[baseTypeId].Add(persdata.TypeId);

            return PersDataToItemType(persdata);
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
