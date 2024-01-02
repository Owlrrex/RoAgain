using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "MapPrefabTable", menuName = "ScriptableObjects/MapPrefabTable", order = 2)]
    public class MapPrefabTable : ScriptableObject
    {
        public static MapPrefabTable Instance;

        [Serializable]
        private class Entry
        {
            public string MapId;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<string, GameObject> _prefabsByName;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register MapPrefabTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate MapPrefabTable!", GameComponent.Other);
                return;
            }

            if (_prefabsByName == null)
            {
                _prefabsByName = new();
                foreach (Entry entry in _entries)
                {
                    _prefabsByName.Add(entry.MapId, entry.Prefab);
                }
            }

            Instance = this;
        }

        public static GameObject GetPrefabById(string mapId)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get Prefab for mapId {mapId} before MapPrefabTable was available", GameComponent.Other);
                return null;
            }

            return Instance._prefabsByName[mapId];
        }
    }
}