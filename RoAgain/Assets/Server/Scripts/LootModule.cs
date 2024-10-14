using OwlLogging;
using System.Collections.Generic;
using Shared;

namespace Server
{
    public class LootModule
    {
        private InventoryModule _inventoryModule;

        private ALootTableDatabase _lootTableDatabase;

        private PickupModule _pickupModule;

        private System.Random _rand = new(); // Use creation time seed

        public int Initialize(ALootTableDatabase lootTableDatabase, InventoryModule inventoryModule, PickupModule pickupModule)
        {
            if(lootTableDatabase == null)
            {
                OwlLogger.LogError("Can't initialize LootModule with null LootTableDatabase!", GameComponent.Items);
                return -1;
            }

            if (inventoryModule == null)
            {
                OwlLogger.LogError("Can't initialize LootModule with null InventoryModule!", GameComponent.Items);
                return -1;
            }

            if (pickupModule == null)
            {
                OwlLogger.LogError("Can't initialize LootModule with null PickupModule!", GameComponent.Items);
                return -1;
            }

            _lootTableDatabase = lootTableDatabase;
            _inventoryModule = inventoryModule;
            _pickupModule = pickupModule;

            return 0;
        }

        public void Shutdown()
        {
            _lootTableDatabase = null;
            _inventoryModule = null;
            _pickupModule = null;
        }

        public void RollAllFromTableAsPickup(int lootTableId, Coordinate coordinates, int ownerId = 0)
        {
            List<LootTableEntry> itemsToCreate = RollAllFromTable(lootTableId);

            foreach (LootTableEntry entry in itemsToCreate)
            {
                _pickupModule.CreatePickup(entry.ItemTypeId, entry.Amount, coordinates, ownerId);
            }
        }

        public void RollAllFromTableToInventory(int lootTableId, int targetInventoryId)
        {
            List<LootTableEntry> itemsToCreate = RollAllFromTable(lootTableId);

            foreach (LootTableEntry entry in itemsToCreate)
            {
                _inventoryModule.AddItemsToInventory(targetInventoryId, entry.ItemTypeId, entry.Amount);
            }
        }

        public void RollFirstFromTableAsPickup(int lootTableId, Coordinate coordinates, int ownerId = 0)
        {
            LootTableEntry entryToCreate = RollFirstFromTable(lootTableId);

            _pickupModule.CreatePickup(entryToCreate.ItemTypeId, entryToCreate.Amount, coordinates, ownerId);
        }

        public void RollFirstFromTableToInventory(int lootTableId, int targetInventoryId)
        {
            LootTableEntry entryToCreate = RollFirstFromTable(lootTableId);

            _inventoryModule.AddItemsToInventory(targetInventoryId, entryToCreate.ItemTypeId, entryToCreate.Amount);
        }

        public List<LootTableEntry> RollAllFromTable(int lootTableId)
        {
            LootTableData table = _lootTableDatabase.GetOrLoadLootTable(lootTableId);
            if(table == null)
            {
                OwlLogger.LogError($"LootTable {lootTableId} not found when trying to roll loot!", GameComponent.Items);
                return null;
            }

            List<LootTableEntry> results = new();
            foreach(LootTableEntry entry in table.Entries)
            {
                // Not using <= here so that when Chance == 0, NextDouble() can't create a true value by return 0.
                // <= would cover a potential match at 1.0 correctly, though according to NextDouble() docs, it can never return 1.0
                // In theory, this might have a chance to "Gen1 miss", and Chances of 0 shouldn't be valid = never reach this point
                // Monitor drops of 100% items to see if this needs adjusting
                if (_rand.NextDouble() < entry.Chance)
                {
                    results.Add(entry);
                }
            }
            return results;
        }

        public LootTableEntry RollFirstFromTable(int lootTableId)
        {
            LootTableData table = _lootTableDatabase.GetOrLoadLootTable(lootTableId);
            if (table == null)
            {
                OwlLogger.LogError($"LootTable {lootTableId} not found when trying to roll loot!", GameComponent.Items);
                return null;
            }

            foreach (LootTableEntry entry in table.Entries)
            {
                // Not using <= here so that when Chance == 0, NextDouble() can't create a true value by return 0.
                // <= would cover a potential match at 1.0 correctly, though according to NextDouble() docs, it can never return 1.0
                // In theory, this might have a chance to "Gen1 miss", and Chances of 0 shouldn't be valid = never reach this point
                // Monitor drops of 100% items to see if this needs adjusting
                if (_rand.NextDouble() < entry.Chance)
                {
                    return entry;
                }
            }

            return null;
        }

        public void HandleLoot(Mob mob)
        {
            if (mob.LootTableId <= 0)
                return;

            List<float> thresholds = new();
            List<CharacterRuntimeData> characters = new();
            float lastThreshold = 0f;
            foreach (KeyValuePair<int, int> kvp in mob.BattleContributions)
            {
                if (!AServer.Instance.TryGetLoggedInCharacterByEntityId(kvp.Key, out CharacterRuntimeData contributor))
                {
                    // TODO: distinguish between "entity not found" and "contributions from non-character"?
                    // Non-Characters can currently not gain any loot
                    continue;
                }

                // TODO: Other Reasons for loot-inegibility (afk-timer, wrong map, etc) here

                float ratio = kvp.Value / (float)mob.MaxHp.Total;
                lastThreshold += ratio;
                thresholds.Add(lastThreshold);
                characters.Add(contributor);
            }

            // normalize the thresholds to account for Contributions not adding up to 100%
            float thresholdFactor = 1f / lastThreshold;
            for (int i = 0; i < thresholds.Count; i++)
            {
                thresholds[i] *= thresholdFactor;
            }

            List<LootTableEntry> items = RollAllFromTable(mob.LootTableId);

            // TODO: Different Party-Loot-rules
            foreach (LootTableEntry item in items)
            {
                double roll = _rand.NextDouble();
                for (int i = 0; i < thresholds.Count; i++)
                {
                    if (roll < thresholds[i])
                    {
                        bool giveLootToInventory = _inventoryModule.HasPlayerSpaceForItemStack(characters[i], item.ItemTypeId, item.Amount);
                        giveLootToInventory = false;
                        // TODO: Handle player's and/or server's autoloot-config
                        if (giveLootToInventory)
                        {
                            _inventoryModule.AddItemsToCharacterInventory(characters[i], item.ItemTypeId, item.Amount);
                        }
                        else
                        {
                            _pickupModule.QueuePickupCreation(item.ItemTypeId, item.Amount, mob.Coordinates, characters[i].Id);
                        }
                        break;
                    }
                }
            }
        }
    }
}

