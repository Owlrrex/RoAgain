using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class LootModule
    {
        private InventoryModule _inventoryModule;

        private ALootTableDatabase _lootTableDatabase;

        public int Initialize(ALootTableDatabase lootTableDatabase, InventoryModule inventoryModule)
        {
            // TODO check null

            _lootTableDatabase = lootTableDatabase;
            _inventoryModule = inventoryModule;

            return 0;
        }

        public void Shutdown()
        {

        }

        public void RollAllFromTableAsPickup(int lootTableId)
        {
            List<LootTableEntry> itemsToCreate = RollAllFromTable(lootTableId);

            foreach (LootTableEntry entry in itemsToCreate)
            {
                // TODO: Create Pickup via Pickup-Module
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

        public void RollFirstFromTableAsPickup(int lootTableId)
        {
            LootTableEntry entryToCreate = RollFirstFromTable(lootTableId);

            // TODO: Create Pickup via Pickup-Module
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
            System.Random r = new(); // Uses clock-based seed
            foreach(LootTableEntry entry in table.Entries)
            {
                // Not using <= here so that when Chance == 0, NextDouble() can't create a true value by return 0.
                // <= would cover a potential match at 1.0 correctly, though according to NextDouble() docs, it can never return 1.0
                // In theory, this might have a chance to "Gen1 miss", and Chances of 0 shouldn't be valid = never reach this point
                // Monitor drops of 100% items to see if this needs adjusting
                if (r.NextDouble() < entry.Chance)
                {
                    results.Add(entry);
                }
            }
            return results;
        }

        public LootTableEntry RollFirstFromTable(int lootTableId)
        {
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
                    // Non-Characters can currently not gain any exp, despite BaseLvls being supported for BattleEntities
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
            System.Random r = new();
            foreach (LootTableEntry item in items)
            {
                double roll = r.NextDouble();
                for (int i = 0; i < thresholds.Count; i++)
                {
                    if (roll < thresholds[i])
                    {
                        // TODO: Handle player's and/or server's autoloot-config
                        if (_inventoryModule.HasPlayerSpaceForItemStack(characters[i], item.ItemTypeId, item.Amount))
                        {
                            // TODO: Use Pickups instead of to-inventory by default

                            _inventoryModule.AddItemsToCharacterInventory(characters[i], item.ItemTypeId, item.Amount);
                        }
                        else
                        {
                            // TODO: Generate pickup with ownership
                        }
                        break;
                    }
                }
            }
        }
    }
}

