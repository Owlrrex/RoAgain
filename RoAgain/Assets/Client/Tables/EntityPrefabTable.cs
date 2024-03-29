using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;
using Shared;

namespace Client
{
    [CreateAssetMenu(fileName = "EntityPrefabTable", menuName = "ScriptableObjects/EntityPrefabTable", order = 3)]
    public class EntityPrefabTable : ScriptableObject
    {
        public static EntityPrefabTable Instance;

        public enum EntityType
        {
            Unknown,
            GenericGrid,
            GenericBattle,
            RemoteCharacter,
            LocalCharacter,
        }

        [Serializable]
        private class Entry
        {
            public EntityType Type;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<EntityType, GameObject> _prefabsByType;
        private Dictionary<JobId, GameObject> _jobPrefabsByJobId;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register EntityPrefabTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate EntityPrefabTable!", GameComponent.Other);
                return;
            }

            if (_prefabsByType == null)
            {
                _prefabsByType = new();
                foreach (Entry entry in _entries)
                {
                    _prefabsByType.Add(entry.Type, entry.Prefab);
                }
            }

            Instance = this;
        }

        public static GameObject GetPrefabForType(EntityType type)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get Prefab for EntityType {type} before EntityPrefabTable was available", GameComponent.Other);
                return null;
            }

            return Instance._prefabsByType[type];
        }
    }
}