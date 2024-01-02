using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "CellEffectTable", menuName = "ScriptableObjects/CellEffectTable", order = 1)]
    public class CellEffectPrefabTable : ScriptableObject
    {
        public static CellEffectPrefabTable Instance;

        [Serializable]
        private class Entry
        {
            public CellEffectType Type;
            public GameObject Prefab;
        }

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<CellEffectType, GameObject> _prefabsByType;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register CellEffectPrefabTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate CellEffectPrefabTable!", GameComponent.Other);
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

        public static GameObject GetPrefabByType(CellEffectType type)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get Prefab for CellEffectType {type} before CellEffectPrefabTable was available", GameComponent.Other);
                return null;
            }

            return Instance._prefabsByType[type];
        }
    }
}