using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "StringTable", menuName = "ScriptableObjects/StringTable", order = 4)]
    public class StringTable : ScriptableObject
    {
        public static StringTable Instance;

        [Serializable]
        private class StringTableEntry
        {
            public int Id;
            public string Text;
        }

        [SerializeField]
        private List<StringTableEntry> _entries;

        private Dictionary<int, string> _stringsById;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register StringTable with null entries!", GameComponent.Other);
                return;
            }

            if (_stringsById == null)
            {
                _stringsById = new();
                foreach (StringTableEntry entry in _entries)
                {
                    _stringsById.Add(entry.Id, entry.Text);
                }
            }

            if (Instance != null)
            {
                OwlLogger.LogError("Duplicate StringTable!", GameComponent.Other);
                return;
            }

            Instance = this;
        }

        // TODO: This one will need an unregister-option to allow players to change localization at runtime

        public static string GetStringById(int id)
        {
            if (Instance == null)
            {
                OwlLogger.LogError("Tried to get String by Id before StringTable was available", GameComponent.Other);
                return null;
            }

            return Instance._stringsById[id];
        }
    }
}