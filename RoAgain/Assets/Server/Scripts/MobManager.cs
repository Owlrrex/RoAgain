using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;
using Shared;

namespace Server
{
    public class Mob : ServerBattleEntity
    {
        public SpawnAreaDefinition SpawnArea;

        public int MobTypeId;
        public int BaseExpReward;
        public int JobExpReward;
        // TODO: Droptable reference?

        public void Init()
        {
            Dex.ValueChanged += OnDexChanged;
            Agi.ValueChanged += OnAgiChanged;
            Vit.ValueChanged += OnVitChanged;
            Int.ValueChanged += OnIntChanged;
            Luk.ValueChanged += OnLukChanged;

            CalculateHit();
            CalculateFlee();
            CalculateSoftDef();
            CalculateSoftMDef();

            CurrentHp = MaxHp.Total;
            CurrentSp = MaxSp.Total;

            if (CurrentHp == 0)
                OwlLogger.LogError($"Mob with TypeId {MobTypeId} has 0 hp on spawn!", GameComponent.Other);

            // TODO: Status resistances

            // TODO: Init AI?
        }

        public void InitFromStaticData(int mobTypeId, MobDataStatic staticData)
        {
            MobTypeId = mobTypeId;

            LocalizedNameId = staticData.DefaultDisplayName;
            ModelId = staticData.ModelId;

            BaseExpReward = staticData.BaseExpReward;
            JobExpReward = staticData.JobExpReward;

            BaseLvl.Value = staticData.BaseLvl;
            Race = staticData.Race;
            Size = staticData.Size;
            Element = staticData.Element;

            MaxHp.SetBase(staticData.MaxHp);
            MaxSp.SetBase(staticData.MaxSp);
            BaseAttackRange.Value = staticData.BaseAttackRange;
            Str.SetBase(staticData.Str);
            Agi.SetBase(staticData.Agi);
            Vit.SetBase(staticData.Vit);
            Int.SetBase(staticData.Int);
            Dex.SetBase(staticData.Dex);
            Luk.SetBase(staticData.Luk);

            MeleeAtkMin.SetBase(staticData.MinAtk);
            RangedAtkMin.SetBase(staticData.MinAtk);
            MeleeAtkMax.SetBase(staticData.MaxAtk);
            RangedAtkMax.SetBase(staticData.MaxAtk);
            MatkMin.SetBase(staticData.MinMatk);
            MatkMax.SetBase(staticData.MaxMatk);
            HardDef.SetBase(staticData.HardDef / 100f);
            HardMDef.SetBase(staticData.HardMDef / 100f);
            // TODO: Aspd
            Movespeed.Value = staticData.Movespeed;

            // TODO: Setup AI

            Init();
        }

        private void OnDexChanged(Stat _)
        {
            CalculateHit();
        }

        private void OnAgiChanged(Stat _)
        {
            CalculateFlee();
        }

        private void OnVitChanged(Stat _)
        {
            CalculateSoftDef();
        }

        private void OnIntChanged(Stat _)
        {
            CalculateSoftMDef();
        }

        private void OnLukChanged(Stat _)
        {
            // TODO: Status Resistances
        }

        public void CalculateHit()
        {
            Hit.SetBase(Dex.Total + BaseLvl.Value);
        }

        public void CalculateFlee()
        {
            Flee.SetBase(Agi.Total + BaseLvl.Value);
        }

        public void CalculateSoftDef()
        {
            SoftDef.SetBase(Vit.Total);
        }

        public void CalculateSoftMDef()
        {
            SoftMDef.SetBase(Int.Total);
        }
    }

    [Serializable]
    public class SpawnAreaDefinition
    {
        public int AreaId;
        public int MobCount;
        public int MobSpeciesId;
        public Vector2Int BoundsMin;
        public Vector2Int BoundsMax;
        public int Delay;
    }

    public class MapMobManager // One per MapInstance
    {
        // TODO later: See how much this breaks when mobs change maps
        private ServerMapInstance _map;

        private readonly Dictionary<int, SpawnAreaDefinition> _mobDefinitions = new();
        private readonly Dictionary<int, List<Mob>> _mobsByAreaId = new();
        private readonly Dictionary<int, List<TimerFloat>> _timersByAreaId = new();

        private readonly List<Mob> _mobsToClear = new();

        private Action<BattleEntity, BattleEntity> _expMobDeathCallback;

        public int Initialize(ServerMapInstance map, ExperienceModule expModule)
        {
            // TODO: Safety checks

            _map = map;

            _expMobDeathCallback = expModule.OnMobDeath;

            LoadSpawnSetForMap("default");

            return 0;
        }

        public void Shutdown()
        {
            foreach (KeyValuePair<int, SpawnAreaDefinition> kvp in _mobDefinitions)
            {
                ClearMobsForDefinition(kvp.Key);
            }

            _mobsByAreaId.Clear();
            _mobDefinitions.Clear();
            _map = null;
        }

        public int LoadSpawnSetForMap(string tag)
        {
            // TODO: Error checks
            List<SpawnAreaDefinition> spawnAreas = SpawnDatabase.GetSpawnAreasForMapId(_map.MapId, tag);
            foreach (SpawnAreaDefinition area in spawnAreas)
            {
                AddSpawnArea(area, false);
            }
            return 0;
        }

        public void AddSpawnArea(SpawnAreaDefinition newDef, bool clearPreviousMobs)
        {
            // TODO safety checks
            if (!_mobDefinitions.ContainsKey(newDef.AreaId))
            {
                _mobsByAreaId.Add(newDef.AreaId, new()); // Add moblist for definitionId that didn't exist before
            }

            _mobDefinitions[newDef.AreaId] = newDef;
            _timersByAreaId[newDef.AreaId] = new();
        }

        public void UpdateMobSpawns(float deltaTime)
        {
            foreach(Mob mob in _mobsToClear)
            {
                ClearMob(mob);
            }
            _mobsToClear.Clear();

            foreach(KeyValuePair<int, List<TimerFloat>> kvp  in _timersByAreaId)
            {
                for(int i = kvp.Value.Count - 1; i >= 0; i--)
                {
                    kvp.Value[i].Update(deltaTime);
                    if (kvp.Value[i].IsFinished())
                        kvp.Value.RemoveAt(i);
                }
            }

            foreach (KeyValuePair<int, SpawnAreaDefinition> defKvp in _mobDefinitions)
            {
                int currentMobCount = _mobsByAreaId[defKvp.Key].Count;
                int currentRespawnCount = _timersByAreaId[defKvp.Key].Count;
                int desiredMobCount = defKvp.Value.MobCount;
                int mobsToSpawnCount = desiredMobCount - currentMobCount - currentRespawnCount;

                for (int i = 0; i < mobsToSpawnCount; i++)
                {
                    int spawnResult = SpawnMob(defKvp.Value);
                    if (spawnResult != 0)
                    {
                        // Log
                    }
                }
            }
        }

        public int SpawnMob(SpawnAreaDefinition spawnArea)
        {
            // TODO safety checks
            Mob mob = CreateNewMob(spawnArea.MobSpeciesId);
            if (mob == null)
                return -2; // Logged inside CreateNewMob()
            mob.SpawnArea = spawnArea;

            Vector2Int spawnPos = _map.Grid.FindRandomPosition(spawnArea.BoundsMin, spawnArea.BoundsMax, false);
            if(spawnPos == GridData.INVALID_COORDS)
            {
                // This _can_ happen by sheer randomness if there's at least one void-cell in the area.
                OwlLogger.Log($"Can't spawn mob for Area {spawnArea.AreaId}: No spawn position found!", GameComponent.Other);
                return -1;
            }
            
            _map.Grid.PlaceOccupant(mob, spawnPos);
            _mobsByAreaId[spawnArea.AreaId].Add(mob);

            return 0;
        }

        private Mob CreateNewMob(int mobTypeId)
        {
            MobDataStatic staticData = MobDatabase.GetMobDataForId(mobTypeId);
            if (staticData == null)
                return null; // logged inside GetMobDataForId()

            // tmp: Use SquareWalkers, to make the map more lively
            SquareWalkerEntity mob = new()
            {
                Id = GridEntity.NextEntityId,
                HpRegenTime = 30,
                SpRegenTime = 30,
            };
            mob.InitFromStaticData(mobTypeId, staticData);

            mob.Death += OnEntityDeath;
            mob.Death += _expMobDeathCallback;
            GridData.Direction nextDirection = UnityEngine.Random.Range(1, 5) switch
            {
                1 => GridData.Direction.North,
                2 => GridData.Direction.East,
                3 => GridData.Direction.South,
                4 => GridData.Direction.West,
                _ => GridData.Direction.North,
            };
            mob.Initialize(UnityEngine.Random.Range(1, 5), nextDirection);

            return mob;
        }

        private void OnEntityDeath(BattleEntity entity, BattleEntity killer)
        {
            if (entity is not Mob mob)
                return;

            _mobsToClear.Add(mob);

            if(mob.SpawnArea != null)
            {
                // Don't read delay from mob.SpawnArea - the SpawnArea may've changed since the mob was spawned
                _timersByAreaId[mob.SpawnArea.AreaId].Add(new(_mobDefinitions[mob.SpawnArea.AreaId].Delay));
            }
        }

        public int ClearMobsForDefinition(int definitionId)
        {
            // TODO safety checks

            int clearedMobCount = 0;
            List<Mob> moblist = _mobsByAreaId[definitionId];
            for (int i = moblist.Count - 1; i >= 0; i--)
            {
                if (ClearMob(moblist[i]) == 0)
                {
                    clearedMobCount++;
                }
                else
                {
                    // Log
                }
            }

            return clearedMobCount;
        }

        public int ClearMob(Mob mob)
        {
            _map.Grid.RemoveOccupant(mob);

            int areaId = mob.SpawnArea.AreaId;
            _mobsByAreaId[areaId].Remove(mob);
            return 0;
        }
    }
}
