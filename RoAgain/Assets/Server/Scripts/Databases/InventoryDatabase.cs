using OwlLogging;
using System;
using Shared;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Server
{
    [Serializable]
    public class InventoryPersistenceData
    {
        public int InventoryId;
        public DictionarySerializationWrapper<long, int> ItemsWrapper;
    }

    public abstract class AInventoryDatabase
    {
        protected int _nextInventoryId = 1;

        public abstract int Initialize(string config);

        public abstract void Shutdown();

        public abstract int Persist(InventoryPersistenceData invPersData);

        public abstract bool DoesInventoryExist(int inventoryId);

        public abstract InventoryPersistenceData LoadInventoryPersistenceData(int inventoryId);

        public abstract int DeleteInventory(int inventoryId);

        public abstract IEnumerable<long> FindAllUsedItemTypeIds();

        public virtual InventoryPersistenceData CreateInventory()
        {
            int newId = _nextInventoryId++;
            OwlLogger.Log($"Creating inventory with id {newId}", GameComponent.Persistence);

            InventoryPersistenceData invPersData = new InventoryPersistenceData()
            {
                InventoryId = newId,
                ItemsWrapper = new()
            };

            int persistResult = Persist(invPersData);

            if(persistResult != 0)
            {
                OwlLogger.LogError($"Error while creating inventory, code {persistResult}!", GameComponent.Persistence);
                return null;
            }

            return LoadInventoryPersistenceData(newId);
        }
    }

    public class InventoryDatabase : AInventoryDatabase
    {
        private const string FILE_SUFFIX = ".invdb";
        private string _folderPath;

        public override int Initialize(string config)
        {
            if (string.IsNullOrEmpty(config))
            {
                OwlLogger.LogError($"Can't initialize InventoryDatabase with empty config", GameComponent.Persistence);
                return -1;
            }

            if (!Directory.Exists(config))
            {
                OwlLogger.Log($"Creating Inventory database folder: {config}", GameComponent.Persistence);
                try
                {
                    Directory.CreateDirectory(config);
                }
                catch (Exception e)
                {
                    OwlLogger.LogError($"Exception while creating Inventory Database folder at {config}: {e.Message}", GameComponent.Persistence);
                    return -2;
                }
            }

            _folderPath = config;

            OwlLogger.Log("Scanning InventoryDatabase for used InventoryIds", GameComponent.Persistence);
            string[] invFiles;
            int invCount = 0;
            try
            {
                invFiles = Directory.GetFiles(_folderPath, "*"+FILE_SUFFIX, SearchOption.TopDirectoryOnly);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while accessing files in Inventory Database: {e.Message}", GameComponent.Persistence);
                return -3;
            }

            foreach (string file in invFiles)
            {
                string filename = Path.GetFileNameWithoutExtension(file);
                if(!int.TryParse(filename, out int invId))
                {
                    OwlLogger.LogError($"Inventory file {file} doesn't have parseable filename!", GameComponent.Persistence);
                    continue;
                }
                _nextInventoryId = Math.Max(_nextInventoryId, invId+1);
                invCount++;
            }
            OwlLogger.LogF("Scanned {0} inventories, next inventoryId is {1}", invCount, _nextInventoryId, GameComponent.Persistence);

            return 0;
        }

        public override void Shutdown()
        {
            _folderPath = null;
            _nextInventoryId = 1;
        }

        public override int DeleteInventory(int inventoryId)
        {
            if(!DoesInventoryExist(inventoryId))
            {
                OwlLogger.LogError($"Tried to delete inventory id {inventoryId} that doesn't exist!", GameComponent.Persistence);
                return -1;
            }

            string path = MakeFilePathForInventory(inventoryId);
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                OwlLogger.LogError($"Exception while deleting inventory {inventoryId}: {ex.Message}", GameComponent.Persistence);
                return -2;
            }

            return 0;
        }

        public override bool DoesInventoryExist(int inventoryId)
        {
            if (inventoryId <= 0)
                return false;

            return File.Exists(MakeFilePathForInventory(inventoryId));
        }

        public override InventoryPersistenceData LoadInventoryPersistenceData(int inventoryId)
        {
            if (!DoesInventoryExist(inventoryId))
            {
                OwlLogger.LogError($"Can't load inventory data for id {inventoryId} - inventory not found", GameComponent.Persistence);
                return null;
            }

            string filePath = MakeFilePathForInventory(inventoryId);

            string rawData;
            try
            {
                rawData = File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Failed to load inventory file {filePath}: {e.Message}", GameComponent.Persistence);
                return null;
            }

            InventoryPersistenceData data = JsonUtility.FromJson<InventoryPersistenceData>(rawData);

            return data;
        }

        public override int Persist(InventoryPersistenceData invPersData)
        {
            if(invPersData == null)
            {
                OwlLogger.LogError("Can't persist a null inventory!", GameComponent.Persistence);
                return -1;
            }

            if(invPersData.InventoryId <= 0)
            {
                OwlLogger.LogError($"Can't persist invalid inventory id {invPersData.InventoryId}", GameComponent.Persistence);
                return -2;
            }

            if(invPersData.ItemsWrapper == null)
            {
                OwlLogger.LogError($"Can't persist inventory with null items, id {invPersData.InventoryId}", GameComponent.Persistence);
                return -3;
            }

            string path = MakeFilePathForInventory(invPersData.InventoryId);
            string data = JsonUtility.ToJson(invPersData);
            try
            {
                File.WriteAllText(path, data);
            }
            catch (Exception e)
            {
                OwlLogger.LogError($"Exception while persisting inventory {invPersData.InventoryId}: {e.Message}", GameComponent.Persistence);
                return -5;
            }

            return 0;
        }

        private string MakeFilePathForInventory(int inventoryId)
        {
            return Path.Combine(_folderPath, $"{inventoryId}{FILE_SUFFIX}");
        }

        public override IEnumerable<long> FindAllUsedItemTypeIds()
        {
            throw new NotImplementedException();
        }
    }
}
