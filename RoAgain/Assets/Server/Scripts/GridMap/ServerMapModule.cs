using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    // This class manages the collective of instances maps. It probably will also interact closely with their occupants
    // It may end up having an equally central role as the original MapServer did. 
    // Try to keep Battle-Code in its own module, though.
    public class ServerMapModule
    {
        private Dictionary<string, ServerMapInstance> _mapInstances = new();

        private ExperienceModule _expModule;

        public int Initialize(ExperienceModule expModule)
        {
            if(expModule == null)
            {
                OwlLogger.LogError($"Can't initialize ServerMapModule with null ExperienceModule!", GameComponent.Other);
                return -1;
            }

            _expModule = expModule;

            return 0;
        }

        public void Update(float deltaTime)
        {
            List<ServerMapInstance> maps = new(_mapInstances.Values);
            foreach(ServerMapInstance instance in maps)
            {
                instance.Update(deltaTime);
            }
        }

        public ServerMapInstance CreateOrGetMap(string mapId)
        {
            if (string.IsNullOrEmpty(mapId))
            {
                OwlLogger.LogError($"Can't create or get map for invalid mapId {mapId}!", GameComponent.Other);
                return null;
            }

            ServerMapInstance instance = GetMapInstance(mapId);
            if(instance == null)
            {
                instance = CreateNewMapInstance(mapId);
            }
            return instance;
        }

        public ServerMapInstance CreateNewMapInstance(string mapId)
        {
            if(string.IsNullOrEmpty(mapId))
            {
                OwlLogger.LogError($"Can't create map for invalid mapId {mapId}", GameComponent.Other);
                return null;
            }

            if(_mapInstances.ContainsKey(mapId))
            {
                OwlLogger.LogError($"Can't create map for mapId {mapId} - already exists!", GameComponent.Other);
                return null;
            }

            ServerMapInstance newInstance = new();
            newInstance.Initialize(mapId, _expModule);
            _mapInstances.Add(mapId, newInstance);
            return newInstance;
        }

        public ServerMapInstance GetMapInstance(string mapId)
        {
            ServerMapInstance mapInstance;
            if(_mapInstances.TryGetValue(mapId, out mapInstance))
            {
                return mapInstance;
            }
            return null;
        }

        public bool HasMapInstance(string mapId)
        {
            return _mapInstances.ContainsKey(mapId);
        }

        public void DestroyMapInstance(string mapId)
        {
            if(_mapInstances.ContainsKey(mapId))
            {
                _mapInstances[mapId].Shutdown();
                _mapInstances.Remove(mapId);
            }
        }

        public GridEntity FindEntityOnAllMaps(int entityId)
        {
            GridEntity result = null;
            foreach(ServerMapInstance map in _mapInstances.Values)
            {
                result = map.Grid.FindOccupant(entityId);
                if (result != null)
                    break;
            }
            return result;
        }

        public int MoveEntityBetweenMaps(int entityId, string sourceMapId, string targetMapId, Vector2Int targetCoordinates)
        {
            if (entityId <= 0)
            {
                OwlLogger.LogError($"Map move failed - invalid entity id {entityId} !", GameComponent.Other);
                return -1;
            }

            if (string.IsNullOrEmpty(sourceMapId) || string.IsNullOrEmpty(targetMapId))
            {
                OwlLogger.LogError($"Map move failed from map {sourceMapId} to {targetMapId} - invalid map ids!", GameComponent.Other);
                return -3;
            }

            ServerMapInstance sourceMap = GetMapInstance(sourceMapId); // don't create source map, it has to exist already
            if (sourceMap == null)
            {
                OwlLogger.LogError($"Map move failed from map {sourceMapId} to {targetMapId} - source map Instance is null!", GameComponent.Other);
                return -4;
            }

            ServerMapInstance targetMap = CreateOrGetMap(targetMapId);
            if (targetMap == null)
            {
                OwlLogger.LogError($"Map move failed from map {sourceMapId} to {targetMapId} - target map Instance is null!", GameComponent.Other);
                return -7;
            }

            if (!targetMap.Grid.AreCoordinatesValid(targetCoordinates))
            {
                OwlLogger.LogError($"Map move failed - target coordinates {targetMapId}@{targetCoordinates} invalid!", GameComponent.Other);
            }

            GridEntity occupant = sourceMap.Grid.FindOccupant(entityId);
            if (occupant == null)
            {
                OwlLogger.LogError($"Map move failed - Entity {entityId} not found on source map {sourceMapId}!", GameComponent.Other);
                return -5;
            }


            if (targetMap == sourceMap)
            {
                sourceMap.Grid.MoveOccupant(occupant, occupant.Coordinates, targetCoordinates);
            }
            else
            {
                if (!sourceMap.Grid.RemoveOccupant(occupant))
                {
                    OwlLogger.LogError($"Map move failed - Remove failed!", GameComponent.Other);
                    return -6;
                }

                if (!targetMap.Grid.PlaceOccupant(occupant, targetCoordinates))
                {
                    OwlLogger.LogError($"Map move failed - Place failed! Entity is now orphaned!!", GameComponent.Other);
                    return -8;
                }
                occupant.MapId = targetMapId;
            }

            occupant.ClearPath();

            List<CharacterRuntimeData> arrivalWitnesses = targetMap.Grid.GetObserversSquare<CharacterRuntimeData>(targetCoordinates);
            foreach (CharacterRuntimeData witness in arrivalWitnesses)
            {
                witness.NetworkQueue.GridEntityDataUpdate(occupant);
            }

            bool arePlayersOnSource = false;
            foreach (GridEntity entity in sourceMap.Grid.GetAllOccupants())
            {
                if (entity is CharacterRuntimeData)
                {
                    arePlayersOnSource = true;
                    break;
                }
            }
            if (!arePlayersOnSource)
            {
                DestroyMapInstance(sourceMapId);
            }

            return 0;
        }

        public void Shutdown()
        {
            foreach(ServerMapInstance mapInstance in _mapInstances.Values)
            {
                mapInstance.Shutdown();
            }
            _mapInstances = null;
        }
    }
}
