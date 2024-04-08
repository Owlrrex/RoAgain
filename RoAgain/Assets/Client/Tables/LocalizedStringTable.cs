using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public enum ClientLanguage
    {
        Unknown,
        English,
        German
    }

    // TODO: Rework into a more generic file format so non-unity tools can easier digest localized strings
    [CreateAssetMenu(fileName = "StringTable", menuName = "ScriptableObjects/StringTable", order = 4)]
    public class LocalizedStringTable : ScriptableObject
    {
        private static LocalizedStringTable _instance;

        [Serializable]
        private class StringTableEntry
        {
#pragma warning disable CS0649 // Field 'StringTable.StringTableEntry.Id' is never assigned to, and will always have its default value 0
            public int Id;
#pragma warning restore CS0649 // Field 'StringTable.StringTableEntry.Id' is never assigned to, and will always have its default value 0
            public string TextEnglish;
            public string TextGerman;
        }

        [SerializeField]
        private List<StringTableEntry> _entries;

        private Dictionary<int, string> _stringsById;

        private static ClientLanguage _currentLanguage;

        public void Register()
        {
            if (_entries == null)
            {
                OwlLogger.LogError($"Can't register StringTable with null entries!", GameComponent.Other);
                return;
            }

            if (_instance != null)
            {
                if (_instance == this)
                    OwlLogger.LogError("Duplicate StringTable!", GameComponent.Other);
                else
                    OwlLogger.LogError($"Tried to register StringTable {name} while another is registered: {_instance.name}", GameComponent.Other);
                return;
            }

            if(_currentLanguage != ClientLanguage.Unknown)
            {
                LoadStringsForCurrentLanguage();
            }

            _instance = this;
        }

        public static void SetClientLanguage(ClientLanguage newLanguage)
        {
            if (_currentLanguage == newLanguage)
                return;

            if (_instance != null)
                _instance.LoadStringsForCurrentLanguage();
        }

        private void LoadStringsForCurrentLanguage()
        {
            _stringsById ??= new();

            _stringsById.Clear();
            foreach (StringTableEntry entry in _entries)
            {
                string text = _currentLanguage switch
                {
                    ClientLanguage.English => entry.TextEnglish,
                    ClientLanguage.German => entry.TextGerman,
                    _ => null
                };
                if(string.IsNullOrEmpty(text))
                {
                    text = $"MISSING_TEXT_{_currentLanguage}_{entry.Id}";
                }

                _stringsById.Add(entry.Id, text);
            }
        }

        public static string GetStringById(int id)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get String by Id before StringTable was available", GameComponent.Other);
                return null;
            }

            if (!_instance._stringsById.ContainsKey(id))
                return "MISSING-STRING-" + id;

            return _instance._stringsById[id];
        }
    }
}