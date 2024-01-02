using OwlLogging;
using System.Collections.Generic;

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
