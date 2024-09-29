using OwlLogging;
using Shared;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    public class CharacterEnumerator : IEnumerator<CharacterRuntimeData>
    {
        private HashSet<CharacterRuntimeData>.Enumerator _subEnumerator;

        public CharacterRuntimeData Current => _subEnumerator.Current;

        object IEnumerator.Current => _subEnumerator.Current;

        public CharacterEnumerator(HashSet<CharacterRuntimeData> characters)
        {
            if (characters == null)
            {
                OwlLogger.LogError("Can't initialize CharacterEnumerator with null character!", GameComponent.Other);
                return;
            }
            _subEnumerator = characters.GetEnumerator();
        }

        public void Dispose()
        {
            _subEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return _subEnumerator.MoveNext();
        }

        public void Reset()
        {
            throw new System.NotSupportedException();
        }
    }

    // This class manages a single server-side instance of a map
    public class MapInstance
    {
        public GridData Grid { get; private set; }
        public string MapId { get; private set; }

        public BattleModule BattleModule { get; private set; }

        public MapMobManager MobManager { get; private set; }

        public SkillModule SkillModule { get; private set; }

        public PickupModule PickupModule { get; private set; }

        public LootModule LootModule { get; private set; }

        private HashSet<GridEntity> _gainedEntityBuffer = new();
        private HashSet<GridEntity> _stayedEntityBuffer = new();
        private HashSet<GridEntity> _lostEntityBuffer = new();

        private HashSet<CellEffectGroup> _newVisibleEffectBuffer = new();
        private HashSet<CellEffectGroup> _oldVisibleEffectBuffer = new();
        private HashSet<CellEffectGroup> _noLongerVisibleEffectBuffer = new();

        private HashSet<CharacterRuntimeData> _charactersOnMap = new();

        public int Initialize(string mapId, ExperienceModule expModule, ALootTableDatabase lootDb, InventoryModule inventoryModule)
        {
            MapId = mapId;
            int loadError = LoadGridFromFile(MapId);
            if (loadError != 0)
                return loadError;

            // TODO: Check errorcodes
            BattleModule = new();
            BattleModule.Initialize(this);

            PickupModule = new();
            PickupModule.Initialize(this, inventoryModule);

            LootModule = new();
            LootModule.Initialize(lootDb, inventoryModule, PickupModule);

            MobManager = new();
            MobManager.Initialize(this, expModule, LootModule);

            SkillModule = new();
            SkillModule.Initialize(this);

            return loadError;
        }

        private int LoadGridFromFile(string mapid)
        {
            if (Grid != null)
                DetachFromGrid();

            Grid = GridData.LoadMapFromFiles(mapid);
            if (Grid == null)
            {
                return -1;
            }

            SetupWithGrid();

            return 0;
        }

        private void SetupWithGrid()
        {
            Grid.EntityPlaced += OnEntityPlaced;
            Grid.EntityRemoved += OnEntityRemoved;
        }

        private void DetachFromGrid()
        {
            Grid.EntityPlaced -= OnEntityPlaced;
            Grid.EntityRemoved -= OnEntityRemoved;
        }

        private void OnEntityPlaced(GridEntity entity, Vector2Int coords)
        {
            entity.MapId = MapId;
            if (entity is CharacterRuntimeData data)
            {
                _charactersOnMap.Add(data);
                //UpdateVisibleEntities(data);
            }
            // No need to send packet here, will be discovered as part of the VisibleEntities-update automatically
        }

        private void OnEntityRemoved(GridEntity entity, Vector2Int coords)
        {
            if (entity is CharacterRuntimeData data)
            {
                _charactersOnMap.Remove(data);
            }

            Packet packet = entity.ToRemovedPacket();
            if (packet == null)
                return;

            foreach (CharacterRuntimeData obs in Grid.GetOccupantsInRangeSquareLowAlloc<CharacterRuntimeData>(coords, GridData.MAX_VISION_RANGE))
            {
                obs.Connection.Send(packet);
            }
        }

        public void Update(float deltaTime)
        {
            if (Grid == null)
                return;

            UpdateEntityMovementStupid(deltaTime);

            if (Grid == null)
                return; // Instance was shutdown because all players moved out of it

            ICollection<GridEntity> entityList = Grid.GetAllOccupants();
            foreach (var entity in entityList)
            {
                if (entity is ServerBattleEntity sbe)
                    sbe.Update?.Invoke(sbe, deltaTime);
            }

            PickupModule?.UpdatePickups(deltaTime); // Update Pickups first to reduce the chance of updating pickups generated this frame immediately

            SkillModule?.UpdateSkillExecutions(deltaTime);
            BattleModule?.UpdateRegenerations(deltaTime);
            Grid?.UpdateCellEffects(deltaTime);
            MobManager?.UpdateMobSpawns(deltaTime);

            foreach (CharacterRuntimeData character in _charactersOnMap)
            {
                UpdateVisibleEntities(character);
                UpdateVisibleCellGroups(character);
            }
        }

        // We do as little clever "filtering" here as reasonable, leave optimization for later
        private void UpdateEntityMovementStupid(float deltaTime)
        {
            List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);

            // Entities in newly placed effects will be handled in AddEffect/UpdateCellEffects
            foreach (GridEntity entity in movedEntities)
            {
                if (Grid == null)
                    break; // Grid was destroyed when previous entity moved, probably off-grid

                GridCellData oldCell = Grid.GetDataAtCoords(entity.LastUpdateCoordinates);
                GridCellData newCell = Grid.GetDataAtCoords(entity.Coordinates);

                List<CellEffectGroup> oldEffects = oldCell.GetCellEffects();
                List<CellEffectGroup> newEffects = newCell.GetCellEffects();

                List<CellEffectGroup> leftEffects = new();
                List<CellEffectGroup> stayedEffects = new();
                List<CellEffectGroup> enteredEffects = new();

                bool[] oldEffectsStayed = new bool[oldEffects.Count];

                for (int i = 0; i < newEffects.Count; i++)
                {
                    if (oldEffects.Contains(newEffects[i]))
                    {
                        stayedEffects.Add(newEffects[i]);
                        oldEffectsStayed[i] = true;
                    }
                    else
                        enteredEffects.Add(newEffects[i]);
                }

                for (int i = 0; i < oldEffects.Count; i++)
                {
                    if (!oldEffectsStayed[i])
                        leftEffects.Add(oldEffects[i]);
                }

                foreach (CellEffectGroup leftEffect in leftEffects)
                {
                    leftEffect.EntityLeft(entity);
                }

                foreach (CellEffectGroup enteredEffect in enteredEffects)
                {
                    enteredEffect.EntityEntered(entity);
                }
            }
        }

        // This function tries to filter beforehand which characters to update visibility for, but it doesn't work well yet
        private void UpdateEntityMovementBrokenFilter(float deltaTime)
        {
            List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);
            HashSet<CharacterRuntimeData> charactersToUpdate = new();

            foreach (GridEntity movedEntity in movedEntities)
            {
                charactersToUpdate.UnionWith(Grid.GetOccupantsInRangeSquareLowAlloc<CharacterRuntimeData>(movedEntity.Coordinates, GridData.MAX_VISION_RANGE));
            }

            foreach (CharacterRuntimeData character in charactersToUpdate)
            {
                UpdateVisibleEntities(character);
            }
        }

        private void UpdateVisibleEntities(CharacterRuntimeData character)
        {
            character.RecalculateVisibleEntities(ref _gainedEntityBuffer, ref _stayedEntityBuffer, ref _lostEntityBuffer);

            // Allow other modules to modify visibilty
            PickupModule?.UpdateEntityVisibility(character, ref _gainedEntityBuffer, ref _stayedEntityBuffer, ref _lostEntityBuffer);
            // TODO: Buff-Module to make hidden units invisible
            // NPC-module, for some Npc state?

            // We could also remove _noLongervisible elements here, but that is a) probably slower for a List b) could carry corrupt data over several updates
            character.VisibleEntities.Clear();
            character.VisibleEntities.AddRange(_gainedEntityBuffer);
            character.VisibleEntities.AddRange(_stayedEntityBuffer);

            foreach (GridEntity newEntity in _gainedEntityBuffer)
            {
                // For a unit entering update-range (could be first time): Send full data update
                character.NetworkQueue.GridEntityDataUpdate(newEntity);
            }

            foreach (GridEntity entity in _stayedEntityBuffer)
            {
                if (entity.HasNewPath)
                {
                    // For unit only updating its path: Send only path update
                    character.NetworkQueue.GridEntityPathUpdate(entity);
                }
            }
        }

        private void UpdateVisibleCellGroups(CharacterRuntimeData character)
        {
            character.VisibleCellEffectGroups = character.RecalculateVisibleCellEffectGroups(ref _newVisibleEffectBuffer, ref _oldVisibleEffectBuffer, ref _noLongerVisibleEffectBuffer);

            foreach (CellEffectGroup newGroup in _newVisibleEffectBuffer)
            {
                CellEffectGroupPlacedPacket packet = new()
                {
                    GroupId = newGroup.Id,
                    Shape = newGroup.Shape,
                    Type = newGroup.Type
                };
                character.Connection.Send(packet);
            }

            foreach (CellEffectGroup removedGroup in _noLongerVisibleEffectBuffer)
            {
                CellEffectGroupRemovedPacket packet = new()
                { GroupId = removedGroup.Id };
                character.Connection.Send(packet);
            }
        }

        // Since this provides reference-access, this is scary. Someone could mess with the list. Only use this if ABSOLUTELY needed
        //public HashSet<CharacterRuntimeData> GetAllCharactersOnMap()
        //{
        //    return _charactersOnMap;
        //}

        public CharacterEnumerator GetCharacterEnumerator()
        {
            return new(_charactersOnMap);
        }

        public void Shutdown()
        {
            BattleModule?.Shutdown();

            MobManager?.Shutdown();

            BattleModule = null;
            MobManager = null;
            Grid = null;
            MapId = null;
        }
    }
}