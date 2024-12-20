using OwlLogging;
using Shared;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class MapModule
    {
        private string _currentMapId;
        public GridComponent Grid { get; private set; }

        private GameObject _currentMapInstance;

        private readonly Dictionary<int, GridEntityMover> _displayedGridEntities = new(50);
        private readonly List<GridEntity> _entitiesAwaitingRemoval = new();
        private readonly List<PickupModel> _animatingPickupModels = new();

        private readonly Dictionary<int, CellEffectDisplay> _displayedCellEffects = new(20);
        private readonly List<CellEffectData> _queuedCellEffects = new();

        private readonly List<SkillId> _skillIdsFinishedReuse = new(4); // Not predicted that a unit finishes more cooldowns than this on a single tick

        private HashSet<GridEntity> _newVisibleBuffer = new();
        private HashSet<GridEntity> _oldVisibleBuffer = new();
        private HashSet<GridEntity> _noLongerVisibleBuffer = new();

        public int Initialize()
        {
            return 0;
        }

        public bool IsReady()
        {
            return Grid != null && !string.IsNullOrEmpty(_currentMapId);
        }

        public void Update()
        {
            if (Grid != null && Grid.Data != null)
            {
                // If timing-sync-logic is separate to allow clients to do local latency compensation: Call timing-update here

                Grid.Data.UpdateEntityMovment(Time.deltaTime);

                ClientMain.Instance.CurrentCharacterData.RecalculateVisibleEntities(ref _newVisibleBuffer, ref _oldVisibleBuffer, ref _noLongerVisibleBuffer);
                ClientMain.Instance.CurrentCharacterData.VisibleEntities.Clear();
                ClientMain.Instance.CurrentCharacterData.VisibleEntities.AddRange(_newVisibleBuffer);
                ClientMain.Instance.CurrentCharacterData.VisibleEntities.AddRange(_oldVisibleBuffer);

                foreach (GridEntity entity in _noLongerVisibleBuffer)
                {
                    if (!_entitiesAwaitingRemoval.Contains(entity))
                    {
                        _entitiesAwaitingRemoval.Add(entity);
                    }
                }

                for (int i = _entitiesAwaitingRemoval.Count - 1; i >= 0; i--)
                {
                    if (_entitiesAwaitingRemoval[i].HasFinishedPath())
                    {
                        RemoveMoverOnly(_entitiesAwaitingRemoval[i]);
                        _entitiesAwaitingRemoval.RemoveAt(i);
                    }
                }

                foreach (GridEntity entity in _newVisibleBuffer)
                {
                    if (entity == ClientMain.Instance.CurrentCharacterData)
                        continue;

                    if (!_displayedGridEntities.ContainsKey(entity.Id))
                    {
                        CreateDisplayForEntity(entity);
                    }
                    else
                    {
                        _entitiesAwaitingRemoval.Remove(entity);
                    }
                }

                foreach (GridEntity entity in Grid.Data.GetAllOccupants())
                {
                    if (entity is not ClientBattleEntity bEntity)
                        continue;

                    // tick casttimes
                    bEntity.UpdateSkills(Time.deltaTime);

                    // No need to handle Queued skill on client side - client does not delay sending skill requests

                    for (int i = bEntity.CurrentlyResolvingSkills.Count - 1; i >= 0; i--)
                    {
                        ASkillExecution skill = bEntity.CurrentlyResolvingSkills[i];

                        // remove skills that have finished casting from list
                        // When a skill transitions from casting to animating (=anim-cd), it will be re-added via a SkillExecution packet
                        if (skill.CastTime.IsFinished())
                        {
                            if (skill.AnimationCooldown.IsFinished())
                            {
                                bEntity.CurrentlyResolvingSkills.RemoveAt(i);
                            }
                            else
                            {
                                skill.HasExecutionStarted = true;
                            }
                        }
                    }

                    bEntity.UpdateAnimationLockedState();

                    // Tick & remove skill-cooldowns (used for UI)
                    _skillIdsFinishedReuse.Clear();
                    foreach (KeyValuePair<SkillId, TimerFloat> kvp in bEntity.SkillCooldowns)
                    {
                        kvp.Value.Update(Time.deltaTime);
                        if (kvp.Value.IsFinished())
                            _skillIdsFinishedReuse.Add(kvp.Key);
                    }

                    foreach (SkillId skillId in _skillIdsFinishedReuse)
                    {
                        bEntity.SkillCooldowns.Remove(skillId);
                    }
                }

                for(int i = _animatingPickupModels.Count -1; i >= 0; i--)
                {
                    if (_animatingPickupModels[i].IsPickupAnimationFinished())
                    {
                        RemoveMoverAndEntity(_animatingPickupModels[i].PickupId);
                        _animatingPickupModels.RemoveAt(i);
                    }
                }
            }
        }

        public int SetCurrentMap(string mapId)
        {
            if (mapId == _currentMapId)
                return 0;

            if (_currentMapInstance != null)
            {
                int destroyResult = DestroyCurrentMap();
            }

            int createResult = CreateCurrentMap(mapId);

            _currentMapId = mapId;

            return 0;
        }

        public int DestroyCurrentMap()
        {
            if (_currentMapInstance == null)
                return -1;

            _entitiesAwaitingRemoval.Clear();

            List<int> keys = new(_displayedGridEntities.Keys);
            foreach (int key in keys)
            {
                int removeResult = RemoveMoverAndEntity(key);
            }
            _displayedGridEntities.Clear();

            keys = new(_displayedCellEffects.Keys);
            foreach (int key in keys)
            {
                OnCellEffectGroupRemoved(key);
            }
            _displayedCellEffects.Clear();

            Object.Destroy(_currentMapInstance);
            Grid = null;
            _currentMapId = null;
            _currentMapInstance = null;

            return 0;
        }

        private int CreateCurrentMap(string mapId)
        {
            GameObject mapPrefab = MapPrefabTable.GetPrefabById(mapId);
            if (mapPrefab == null)
            {
                OwlLogger.LogError($"MapPrefab for {mapId} is null!", GameComponent.Other);
                return -1;
            }

            _currentMapInstance = Object.Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            Grid = _currentMapInstance.GetComponent<GridComponent>();
            if (Grid == null)
            {
                OwlLogger.LogError($"Grid Component not found on GameObject for map {mapId}!", GameComponent.Other);
                return -2;
            }

            _currentMapId = mapId;

            Grid.Initialize(mapId);

            foreach (CellEffectData data in _queuedCellEffects)
            {
                OnCellEffectGroupPlaced(data);
            }
            _queuedCellEffects.Clear();

            return 0;
        }

        public void OnGridEntityData(GridEntityData data)
        {
            if (data.MapId != _currentMapId)
            {
                OnEntityRemoved(data.EntityId);
                return;
            }

            if (Grid.Data.FindOccupant(data.EntityId) == null)
            {
                GridEntity check = CreateNewEntity(data);
                if (check == null)
                {
                    OwlLogger.LogError($"Creating GridEntity failed for EntityData, EntityId = {data.EntityId}", GameComponent.Other);
                    return;
                }
                else
                {
                    OwlLogger.LogF("Created Entity Display for entity {0}", data.EntityId, GameComponent.Other, LogSeverity.Verbose);
                }
            }
            else
            {
                OwlLogger.LogF("Updating data for existing entity {0}", data.EntityId, GameComponent.Other, LogSeverity.VeryVerbose);
            }

            UpdateExistingEntityData(data);
        }

        public void OnUnitMovement(UnitMovementInfo moveInfo)
        {
            if (moveInfo == null)
            {
                OwlLogger.LogError("Updated movement for OtherUnit null!", GameComponent.Other);
                return;
            }

            if (moveInfo.UnitId == ClientMain.Instance.CurrentCharacterData.Id)
            {
                OnCharacterMovementReceived(moveInfo.Path);
                return;
            }

            GridEntity movedEntity = Grid.Data.FindOccupant(moveInfo.UnitId);
            if (movedEntity == null)
            {
                OwlLogger.LogError($"Received movementInfo for unit {moveInfo.UnitId} that's not in GridData!", GameComponent.Other);
                return;
            }

            if (!_displayedGridEntities.ContainsKey(moveInfo.UnitId))
            {
                OwlLogger.LogError($"Received movement Info for unit {moveInfo.UnitId} that's not displayed!", GameComponent.Other);
                if (moveInfo.Path.AllCells.Count > 0)
                {
                    int range = ClientMain.Instance.CurrentCharacterData.Coordinates.GridDistanceSquare(moveInfo.Path.AllCells[0]);
                    OwlLogger.LogError($"At range {range}", GameComponent.Other);
                }
                else
                {
                    OwlLogger.LogError($"Empty path, can't calculate range", GameComponent.Other);
                }
                return;
            }

            if (moveInfo.Path == null || moveInfo.Path.AllCells.Count == 0)
            {
                // Serialization artifact - null path may be deserialized as empty arrays
                movedEntity.ClearPath();
                return;
            }

            if (moveInfo.Path.AllCells.Count == 1)
            {
                OwlLogger.LogWarning($"Client received a path of 1 cell length - these shouldn't be sent!", GameComponent.Other);
            }

            Coordinate coordinatesAlongPath = moveInfo.Path.AllCells[0];
            Grid.Data.MoveOccupant(movedEntity, movedEntity.Coordinates, coordinatesAlongPath);
            movedEntity.SetPath(moveInfo.Path, 0, true);
        }

        private void OnCharacterMovementReceived(GridData.Path path)
        {
            int pathCellIndex = 0;
            if (path.AllCells.Count > 0)
            {
                ForceUnitToCoords(ClientMain.Instance.CurrentCharacterData, path.AllCells[0]);
            }
            else
            {
                pathCellIndex = -1;
            }

            OwlLogger.Log($"OnCharacterMovementReceived", GameComponent.Network, LogSeverity.VeryVerbose);
            ClientMain.Instance.CurrentCharacterData.SetPath(path, pathCellIndex, true);
        }

        public void OnEntityRemoved(int entityId)
        {
            if (Grid == null)
                return;

            GridEntity entity = Grid.Data.FindOccupant(entityId);
            if (entity == null)
            {
                OwlLogger.LogWarning($"Received EntityRemoved for entity not on current grid!", GameComponent.Other);
                return;
            }

            int result = RemoveMoverAndEntity(entity);
            if (result != 0)
            {
                OwlLogger.LogError($"Removing entity after EntityRemovedPacket failed: {result}", GameComponent.Other);
            }
        }

        public void UpdateExistingEntityData(GridEntityData data)
        {
            if (Grid == null)
            {
                OwlLogger.LogError($"GridEntityData received while Grid was null - can't add entity!", GameComponent.Other);
                return;
            }

            // Clear "Awaiting Removal" status from entity
            for (int i = _entitiesAwaitingRemoval.Count - 1; i >= 0; i--)
            {
                if (_entitiesAwaitingRemoval[i].Id == data.EntityId)
                    _entitiesAwaitingRemoval.RemoveAt(i);
            }

            Coordinate coords = data.Coordinates;
            GridEntity entity = Grid.Data.GetOccupantFromCell(coords, data.EntityId);

            if (entity == null)
            {
                entity = Grid.Data.FindOccupant(data.EntityId);
                if (entity == null)
                {
                    OwlLogger.LogError($"Tried to update existing entity data for id {data.EntityId}, entity missing!", GameComponent.Other);
                    return;
                }
                // this case will happen if an Entity leaves update-range & then re-enters it in a different position,
                // but before the client-side display is destroyed
                OwlLogger.Log($"Entity Id {data.EntityId} not found on expected cell {coords} - compensating!", GameComponent.Other, LogSeverity.VeryVerbose);
            }

            // SyncPosition to server data
            Grid.Data.MoveOccupant(entity, entity.Coordinates, data.Coordinates, true, false);

            // Update local data that doesn't need special action taken from it
            if (entity is LocalCharacterEntity localChar && data is LocalCharacterData lData)
            {
                localChar.SetData(lData);
                localChar.SkillTreeUpdated?.Invoke();
            }
            else if (entity is RemoteCharacterEntity remoteChar && data is RemoteCharacterData rData)
            {
                remoteChar.SetData(rData);
            }
            else if (entity is ClientBattleEntity bEntity && data is BattleEntityData bData)
            {
                bEntity.SetData(bData);
            }
            else
            {
                entity.SetPath(data.Path, data.PathCellIndex, true);
                entity.Movespeed.Value = data.Movespeed;
                entity.Orientation = data.Orientation;
                entity.ModelId = data.ModelId;
            }
        }

        public GridEntity CreateNewEntity(GridEntityData entityInfo)
        {
            if (Grid == null)
            {
                OwlLogger.LogError("Tried to create new Entity while Grid was null!", GameComponent.Other);
                return null;
            }

            if (entityInfo == null)
            {
                OwlLogger.LogError("Can't create Entity for null info", GameComponent.Other);
                return null;
            }

            if (entityInfo.EntityId <= 0)
            {
                OwlLogger.LogError($"Can't create entity for invalid Id {entityInfo.EntityId}", GameComponent.Other);
                return null;
            }

            if (entityInfo.MapId != _currentMapId)
            {
                OwlLogger.LogError($"Can't create entity - map mismatch! Local: {_currentMapId}, Entity: {entityInfo.MapId}", GameComponent.Other);
                return null;
            }

            if (!Grid.Data.AreCoordinatesValid(entityInfo.Coordinates))
            {
                OwlLogger.LogError($"Received EntityDataPacket with invalid Coordinates: {entityInfo.Coordinates}", GameComponent.Other);
                return null;
            }

            if (entityInfo.Movespeed <= 0)
            {
                OwlLogger.LogError($"Received EntityDataPacket with invalid Movespeed: {entityInfo.Movespeed}", GameComponent.Other);
                return null;
            }

            if (entityInfo.Path.AllCells.Count == 1)
            {
                OwlLogger.LogError($"Path of Length 1 sent in EntityDataPacket!", GameComponent.Other);
            }

            // This if is necessary because RemoteChars, BattleEntities & Gridentities all call OnEntityData
            // TODO: Somehow move this branching into a nicely engineered solution
            GridEntity newEntity = entityInfo switch
            {
                RemoteCharacterData rCharData => CreateRemoteCharacterEntity(rCharData),
                BattleEntityData bData => CreateBattleEntity(bData),
                _ => CreateGridEntity(entityInfo)
            };

            if (newEntity.Path != null && newEntity.Path.AllCells.Count > 0)
                Grid.Data.PlaceOccupantFromPath(newEntity);
            else
                Grid.Data.PlaceOccupant(newEntity, newEntity.Coordinates);

            return newEntity;
        }

        public void CreateDisplayForEntity(GridEntity entity)
        {
            if (entity == null)
            {
                OwlLogger.LogError($"Can't create Display for null entity!", GameComponent.Other);
                return;
            }

            if (_displayedGridEntities.ContainsKey(entity.Id))
            {
                OwlLogger.LogError($"Tried to create Display for entity Id {entity.Id} that already has a Display!", GameComponent.Other);
                return;
            }

            _entitiesAwaitingRemoval.Remove(entity);

            // TODO: Make this type-association nicer, together with refactoring various branches on EntityType / EntityData types
            EntityType entityType = entity switch
            {
                LocalCharacterEntity => EntityType.LocalCharacter,
                RemoteCharacterEntity => EntityType.RemoteCharacter,
                ClientBattleEntity => EntityType.GenericBattle,
                PickupEntity => EntityType.Pickup,
                _ => EntityType.GenericGrid
            };

            GameObject prefab = EntityPrefabTable.GetDataForId(entityType).Prefab;
            GameObject newEntityInstance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity); // TODO: Proper hierarchy
            GridEntityMover newMover = newEntityInstance.GetComponentInChildren<GridEntityMover>();
            if (newMover == null)
            {
                OwlLogger.LogError($"Can't find GridEntityMover on Unit Prefab for Id {entity.Id}!", GameComponent.Other);
                return;
            }

            newMover.Initialize(entity, Grid);
            _displayedGridEntities.Add(entity.Id, newMover);

            if (entity is ClientBattleEntity bEntity)
            {
                BattleEntityModelMain battleModel = newEntityInstance.GetComponent<BattleEntityModelMain>();
                if (battleModel == null)
                {
                    OwlLogger.LogError($"Can't find BattleEntityModelMain on Unit Prefab for Id {entity.Id}, type {entityType}", GameComponent.Other);
                    return;
                }

                battleModel.Initialize(bEntity);
            }
            else if (entity is PickupEntity pickup)
            {
                PickupModel pickupModel = newEntityInstance.GetComponent<PickupModel>();
                if(pickupModel == null)
                {
                    OwlLogger.LogError($"Can't find PickupModel on Pickup Prefab for Id {pickup.Id}", GameComponent.Items);
                    return;
                }
                pickupModel.Initialize(pickup);
                if (pickup.State == PickupState.JustDropped)
                    pickupModel.StartDropAnimation();
            }
        }

        private RemoteCharacterEntity CreateRemoteCharacterEntity(RemoteCharacterData charData)
        {
            RemoteCharacterEntity charEntity = new(charData);
            return charEntity;
        }

        private ClientBattleEntity CreateBattleEntity(BattleEntityData entityData)
        {
            ClientBattleEntity bEntity = new(entityData.Coordinates, entityData.LocalizedNameId, entityData.ModelId, entityData.Movespeed,
                entityData.MaxHp, entityData.MaxSp, entityData.BaseLvl, entityData.EntityId);
            bEntity.SetData(entityData);
            return bEntity;
        }

        private PickupEntity CreatePickupEntity(PickupData data)
        {
            PickupEntity pickup = new(data.Coordinates, data.ItemTypeId, data.Amount, data.RemainingTime, data.OwnerCharacterId);
            pickup.Id = data.PickupId;
            pickup.State = data.State;
            pickup.ModelId = 5; // Pickup model
            return pickup;
        }

        private GridEntity CreateGridEntity(GridEntityData entityData)
        {
            GridEntity newEntity = new(entityData.Coordinates, entityData.LocalizedNameId, entityData.ModelId,
                entityData.Movespeed, entityData.EntityId)
            {
                NameOverride = entityData.NameOverride,
                MapId = entityData.MapId,

                MovementCooldown = entityData.MovementCooldown,
                Orientation = entityData.Orientation,
            };
            newEntity.SetPath(entityData.Path, entityData.PathCellIndex);
            return newEntity;
        }

        public void ForceUnitToCoords(GridEntity entity, Coordinate targetCoords)
        {
            if (Grid == null)
            {
                OwlLogger.LogError($"Tried to force unit {entity.Id} to Coords {targetCoords} while Grid was null!", GameComponent.Other);
                return;
            }

            if (_currentMapId != entity.MapId)
            {
                OwlLogger.LogError($"Can't force entity {entity.Id} to coords {targetCoords} - map mismatch: Local = {_currentMapId}, entity = {entity.MapId}", GameComponent.Other);
                return;
            }

            // TODO upate orientation

            if (targetCoords != entity.Coordinates)
            {
                OwlLogger.Log($"Entity {entity.Id} is being forced from {entity.Coordinates} to {targetCoords}", GameComponent.Other);
                Grid.Data.MoveOccupant(entity, entity.Coordinates, targetCoords, false, false);
                if (_displayedGridEntities.ContainsKey(entity.Id))
                {
                    _displayedGridEntities[entity.Id].SnapToCoordinates(targetCoords);
                }
            }

            
        }

        public int RemoveMoverOnly(GridEntity entity)
        {
            if (entity == null)
                return -2;

            if (!_displayedGridEntities.ContainsKey(entity.Id))
            {
                OwlLogger.Log($"Tried to remove mover entity {entity.Id} that's not displayed!", GameComponent.Other);
                return -1;
            }

            GridEntityMover mover = _displayedGridEntities[entity.Id];
            int shutdownResult = mover.Shutdown();

            _displayedGridEntities.Remove(entity.Id);
            Object.Destroy(mover.gameObject);
            return 0;
        }

        public int RemoveMoverOnly(int entityId)
        {
            GridEntity entity = Grid.Data.FindOccupant(entityId);
            if (entity == null)
            {
                OwlLogger.LogError($"Tried to remove entity Id {entityId} that's not on Grid!", GameComponent.Grid);
                return -1;
            }
                
            return RemoveMoverOnly(entity);
        }

        public int RemoveMoverAndEntity(GridEntity entity)
        {
            if(entity == null)
            {
                return -3;
            }

            if (!_displayedGridEntities.ContainsKey(entity.Id))
            {
                OwlLogger.LogError($"Tried to remove mover entity {entity.Id} that's not displayed!", GameComponent.Other);
                return -1;
            }

            RemoveMoverOnly(entity);
            if (!Grid.Data.RemoveOccupant(entity))
            {
                OwlLogger.LogError($"Failed to remove entity {entity.Id} from Grid, but mover got destroyed!", GameComponent.Other);
                return -2;
            }
            return 0;
        }

        public int RemoveMoverAndEntity(int entityId)
        {
            GridEntity entity = Grid.Data.FindOccupant(entityId);
            return RemoveMoverAndEntity(entity);
        }

        public void OnCellEffectGroupPlaced(CellEffectData data)
        {
            if (!IsReady())
            {
                _queuedCellEffects.Add(data);
                return;
            }

            if (_displayedCellEffects.ContainsKey(data.GroupId))
            {
                OwlLogger.Log($"Updating existing Display for GroupId {data.GroupId}", GameComponent.Other);
                // Update existing EffectDisplay
                return;
            }

            // Currently not needed: Create an actual CellEffectGroup on client-side.
            // The client doesn't actually process the group on the grid

            GameObject prefab = CellEffectPrefabTable.GetDataForId(data.Type).Prefab;
            GameObject instance = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            CellEffectDisplay display = instance.GetComponentInChildren<CellEffectDisplay>();
            if (display == null)
            {
                OwlLogger.LogError($"CellEffectDisplay prefab for effect type {data.Type} doesn't have CellEffectDisplay component!", GameComponent.Other);
                return;
            }

            int result = display.Initialize(data, Grid);
            if (result != 0)
            {
                OwlLogger.LogError($"Error while initialize CellEffectDisplay for type {data.Type}: {result}", GameComponent.Other);
                Object.Destroy(instance);
                return;
            }

            _displayedCellEffects.Add(data.GroupId, display);
        }

        public void OnCellEffectGroupRemoved(int groupId)
        {
            if (!IsReady()
                || !_displayedCellEffects.ContainsKey(groupId))
            {
                // Already shutdown, or not yet loaded
                // Clear any potential queue for this group, and then ignore it
                for (int i = _queuedCellEffects.Count - 1; i >= 0; i--)
                {
                    if (_queuedCellEffects[i].GroupId == groupId)
                        _queuedCellEffects.RemoveAt(i);
                }

                OwlLogger.Log($"Tried to remove CellEffectGroupId {groupId} that wasn't displayed!", GameComponent.Other);
                return;
            }

            CellEffectDisplay display = _displayedCellEffects[groupId];
            display.Shutdown();
            _displayedCellEffects.Remove(groupId);
            Object.Destroy(display.gameObject);
        }

        public void OnPickupDataReceived(PickupData data)
        {
            if (data.State == PickupState.PickedUp
                || data.State == PickupState.AboutToDisappear)
            {
                OwlLogger.LogWarning($"Received pickupData in state {data.State}, omitting.", GameComponent.Items);
                return;
            }

            GridEntity oldPickup = Grid.Data.FindOccupant(data.PickupId);
            if (oldPickup == null)
            {
                PickupEntity newPickup = CreatePickupEntity(data);
                Grid.Data.PlaceOccupant(newPickup, newPickup.Coordinates);
                CreateDisplayForEntity(newPickup);
            }
            else
            {
                PickupEntity pickupEntity = oldPickup as PickupEntity;
                if(pickupEntity == null)
                {
                    OwlLogger.LogError($"Pickup {data.PickupId} exists on grid, but not as PickupEntity!", GameComponent.Items);
                    return;
                }
                pickupEntity.Coordinates = data.Coordinates;
                pickupEntity.Count = data.Amount;
                pickupEntity.ItemTypeId = data.ItemTypeId;
                pickupEntity.LifeTime.Initialize(data.RemainingTime);
                pickupEntity.State = data.State;
                pickupEntity.OwnerEntityId = data.OwnerCharacterId;
            }

            // Clear "Awaiting Removal" status from entity
            for (int i = _entitiesAwaitingRemoval.Count - 1; i >= 0; i--)
            {
                if (_entitiesAwaitingRemoval[i].Id == data.PickupId)
                    _entitiesAwaitingRemoval.RemoveAt(i);
            }
        }

        public void OnPickupRemovedReceived(int pickupId, int pickedUpEntityId)
        {
            GridEntity pickup = Grid.Data.FindOccupant(pickupId);
            if (pickup == null)
            {
                OwlLogger.LogWarning($"Received pickupRemoved for pickup {pickupId} that's not on Grid!", GameComponent.Items);
                return;
            }            

            if (pickedUpEntityId <= 0)
            {
                RemoveMoverAndEntity(pickupId);
                return;
            }

            PickupModel pickupModel = GetComponentFromEntityDisplay<PickupModel>(pickupId);
            _animatingPickupModels.Add(pickupModel);

            Transform targetTransform = null;
            if (pickedUpEntityId == ClientMain.Instance.CurrentCharacterData.Id)
            {
                targetTransform = PlayerMain.Instance.transform;
            }
            else
            {
                GridEntityMover targetMover = null;
                targetMover = GetComponentFromEntityDisplay<GridEntityMover>(pickedUpEntityId);
                if (targetMover == null)
                    return;
                targetTransform = targetMover.transform;
            }
            pickupModel.StartPickupAnimation(targetTransform);
        }

        // Relies on _displayedGridEntities, which doesn't contain local player
        public T GetComponentFromEntityDisplay<T>(int entityId) where T : Component
        {
            if (!_displayedGridEntities.ContainsKey(entityId))
                return null;

            foreach (KeyValuePair<int, GridEntityMover> kvp in _displayedGridEntities)
            {
                if (kvp.Key == entityId)
                {
                    return kvp.Value.gameObject.GetComponent<T>();
                }
            }
            return null;
        }

        // Relies on _displayedGridEntities, which doesn't contain local player
        public void DisplayBaseLvlUpForRemoteEntity(int entityId)
        {
            BattleEntityModelMain model = GetComponentFromEntityDisplay<BattleEntityModelMain>(entityId);
            if (model == null)
            {
                OwlLogger.LogError($"Received BaseLevelUp for entity that's not displayed!", GameComponent.Other);
                return;
            }

            model.SetSkilltext("Base Level UP!", 5);
        }

        // Relies on _displayedGridEntities, which doesn't contain local player
        public void DisplayJobLvlUpForRemoteEntity(int entityId)
        {
            BattleEntityModelMain model = GetComponentFromEntityDisplay<BattleEntityModelMain>(entityId);
            if (model == null)
            {
                OwlLogger.LogError($"Received JobLevelUp for entity that's not displayed!", GameComponent.Other);
                return;
            }

            model.SetSkilltext("Job Level UP!", 5);
        }
    }
}