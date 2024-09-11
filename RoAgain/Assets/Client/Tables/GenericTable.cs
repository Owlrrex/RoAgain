using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class GenericTable<KeyType, DataType, SelfType> : ScriptableObject
        where SelfType : GenericTable<KeyType, DataType, SelfType>
    {
        public static SelfType Instance;

        [Serializable]
        private class Entry
        {
            public KeyType Id;
            public DataType Data;
        }

        [SerializeField]
        private List<Entry> _entries;

        private Dictionary<KeyType, DataType> _dataById;

        public void Register()
        {
            if(_entries == null)
            {
                OwlLogger.LogError($"Can't register {nameof(SelfType)} with null entries!", GameComponent.Other);
                return;
            }

            if (Instance != null)
            {
                OwlLogger.LogError($"Duplicate {nameof(SelfType)}!", GameComponent.Other);
                return;
            }

            if (_dataById == null)
            {
                _dataById = new();
                foreach (Entry entry in _entries)
                {
                    _dataById.Add(entry.Id, entry.Data);
                }
            }

            Instance = (SelfType)this;
        }

        public static DataType GetDataForId(KeyType id)
        {
            if(Instance == null)
            {
                OwlLogger.LogError($"Tried to get Data for Id {id} with no {nameof(SelfType)} available!", GameComponent.Other);
                return default;
            }

            if(!Instance._dataById.ContainsKey(id))
            {
                OwlLogger.LogError($"Tried to get Data for Id {id} that's not found in Table!", GameComponent.Other);
                return default;
            }

            return Instance._dataById[id];
        }
    }
}

