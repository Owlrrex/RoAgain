using System;
using UnityEngine;
using System.Collections.Generic;
using OwlLogging;

namespace Client
{
    [CreateAssetMenu(fileName = "ModelTable", menuName = "ScriptableObjects/ModelTable")]
    public class ModelTable : ScriptableObject
    {
        [Serializable]
        private class Entry
        {
            public int ModelId;
            public GameObject Prefab;
        }

        public static ModelTable Instance;

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<int, GameObject> _prefabsById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register ModelTable with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate ModelTable!", GameComponent.Other);
                return;
            }

            if (_prefabsById == null)
            {
                _prefabsById = new();
                foreach (Entry entry in _entries)
                {
                    _prefabsById.Add(entry.ModelId, entry.Prefab);
                }
            }

            Instance = this;
        }

        public static GameObject GetPrefabForType(int id)
        {
            if (Instance == null)
            {
                OwlLogger.LogError($"Tried to get Prefab for ModelId {id} before ModelTable was available", GameComponent.Other);
                return null;
            }

            return Instance._prefabsById[id];
        }
    }
}

