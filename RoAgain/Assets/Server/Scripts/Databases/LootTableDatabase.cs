using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class LootTableEntry
    {
        public long ItemTypeId;
        public float Chance;
        public int Amount;

        public bool IsValid()
        {
            return ItemTypeId != ItemConstants.ITEM_TYPE_ID_INVALID
                && ItemTypeId > 0
                && Chance > 0f && Chance <= 1f
                && Amount > 0;
        }

        public override string ToString()
        {
            return $"{ItemTypeId}x{Amount}:{Chance:P2}";
        }
    }

    [Serializable]
    public class LootTableData
    {
        public List<LootTableEntry> Entries = new();
    }

    public abstract class ALootTableDatabase
    {
        protected Dictionary<int, LootTableData> _cachedData = new();

        public abstract int Initialize(string config);

        public abstract void Shutdown();

        public LootTableData GetOrLoadLootTable(int lootTableId)
        {
            if(_cachedData.ContainsKey(lootTableId))
                return _cachedData[lootTableId];

            return LoadLootTable(lootTableId);
        }

        protected abstract LootTableData LoadLootTable(int lootTableId);

        public abstract bool DoesLootTableExist(int lootTableId);

        public abstract void UnloadLootTable(int lootTableId);
    }

    public class LootTableDatabase : ALootTableDatabase
    {
        private const string FILE_SUFFIX = ".ltbl";

        private string _folderPath;

        public override int Initialize(string folderPath)
        {
            if(string.IsNullOrEmpty(folderPath))
            {
                OwlLogger.LogError("Can't initialize LootTableDatabase with empty config!", GameComponent.Persistence);
                return -1;
            }

            if(!Directory.Exists(folderPath))
            {
                OwlLogger.Log($"Creating LootTableDatabase folder: {folderPath}", GameComponent.Persistence);
                try
                {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while creating LootTableDatabase folder at {folderPath}: {e.Message}", GameComponent.Persistence);
                    return -2;
                }
            }

            _folderPath = folderPath;

            return 0;
        }

        public override void Shutdown()
        {
            _cachedData.Clear();
            _folderPath = null;
        }

        protected override LootTableData LoadLootTable(int lootTableId)
        {
            if(!DoesLootTableExist(lootTableId))
            {
                OwlLogger.LogError($"Can't load loottable with id {lootTableId} - file not found!", GameComponent.Persistence);
                return null;
            }

            string filePath = MakePathForLootTable(lootTableId);

            string rawData;
            try
            {
                rawData = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Failed to load loottable file {filePath}: {e.Message}", GameComponent.Persistence);
                return null;
            }

            LootTableData data = JsonUtility.FromJson<LootTableData>(rawData);
            for(int i = data.Entries.Count-1; i >= 0; i--)
            {
                LootTableEntry entry = data.Entries[i];
                if (!entry.IsValid())
                {
                    OwlLogger.LogError($"LootTable {lootTableId} contains invalid entry {entry} - removing entry.", GameComponent.Persistence);
                    data.Entries.RemoveAt(i);
                }
            }

            _cachedData[lootTableId] = data;

            return data;
        }

        public override bool DoesLootTableExist(int lootTableId)
        {
            if (lootTableId <= 0)
                return false;

            return File.Exists(MakePathForLootTable(lootTableId));
        }

        public override void UnloadLootTable(int lootTableId)
        {
            _cachedData.Remove(lootTableId);
        }
        
        private string MakePathForLootTable(int lootTableId)
        {
            return Path.Combine(_folderPath, $"{lootTableId}{FILE_SUFFIX}");
        }
    }
}

