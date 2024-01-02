using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Server
{
    [CreateAssetMenu(fileName = "SpawnDatabase", menuName = "ScriptableObjects/SpawnDatabase", order = 5)]
    public class SpawnDatabase : ScriptableObject
    {
        public static SpawnDatabase Instance;

        [Serializable]
        private class SpawnDatabaseEntry
        {
            public string MapId;
            public List<SpawnSet> Sets;
        }

        [Serializable]
        private class SpawnSet
        {
            public string Tag;
            public List<SpawnAreaDefinition> SpawnAreas;
        }

        [SerializeField]
        private List<SpawnDatabaseEntry> _entries;

        private Dictionary<string, List<SpawnSet>> _setsById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register SpawnDatabase with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate SpawnDatabase!", GameComponent.Other);
                return;
            }

            if (_setsById == null)
            {
                _setsById = new();
                foreach (SpawnDatabaseEntry entry in _entries)
                {
                    _setsById.Add(entry.MapId, entry.Sets);
                }
            }

            Instance = this;
        }

        public static List<SpawnAreaDefinition> GetSpawnAreasForMapId(string mapId, string tag)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get SpawnAreas for MapId {mapId} before SpawnDatabase was available", GameComponent.Other);
                return null;
            }

            if (!Instance._setsById.ContainsKey(mapId))
                return new();

            foreach (SpawnSet set in Instance._setsById[mapId])
            {
                if (set.Tag == tag)
                    return set.SpawnAreas;
            }

            OwlLogger.LogError($"Not SpawnSet with tag {tag} found for map {mapId}", GameComponent.Other);
            return null;
        }

        public static bool HasTag(string mapId, string tag)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to query SpawnAreas for MapId {mapId} before SpawnDatabase was available", GameComponent.Other);
                return false;
            }

            foreach (SpawnSet set in Instance._setsById[mapId])
            {
                if (set.Tag == tag)
                    return true;
            }
            return false;
        }
    }
}