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

    /// <summary>
    /// Identifies a Localized String
    /// Only an Int at the moment, but may contain information about multiple string tables or similar in the future
    /// </summary>
    [Serializable]
    public struct LocalizedStringId
    {
        public static readonly LocalizedStringId INVALID = new() { Id = -1 };

        public int Id;
        // Can add stuff like "string bank" here, if that's being added

        public override bool Equals(object obj)
        {
            return obj is LocalizedStringId other && Equals(other);
        }

        public bool Equals(LocalizedStringId other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(LocalizedStringId left, LocalizedStringId right) => left.Equals(right);
        public static bool operator !=(LocalizedStringId left, LocalizedStringId right) => !(left == right);

        public override string ToString()
        {
            return Id.ToString();
        }
    }

    // TODO: Rework into a more generic file format so non-unity tools can easier digest localized strings
    [CreateAssetMenu(fileName = "StringTable", menuName = "ScriptableObjects/StringTable", order = 4)]
    public class LocalizedStringTable : ScriptableObject
    {
        private static LocalizedStringTable _instance;
        private static ClientLanguage _currentLanguage;
        public static event Action LanguageChanged;

        [Serializable]
        private class StringTableEntry
        {
#pragma warning disable CS0649 // Field 'StringTable.StringTableEntry.Id' is never assigned to, and will always have its default value 0
            // Don't use a LocalizedStringId here - if we have multiple StringTables or Contexts or such, they'll likely have a different persistent structure
            public int Id;
#pragma warning restore CS0649 // Field 'StringTable.StringTableEntry.Id' is never assigned to, and will always have its default value 0
            public string TextEnglish;
            public string TextGerman;
        }

        [SerializeField]
        private List<StringTableEntry> _entries;

        // This Dictionary will probably get split up if String-banks are ever being added
        // or LocalizedStringId becomes a more complex type than just a single int
        private Dictionary<int, string> _stringsById;

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

            if (_instance == null)
                return;

            _currentLanguage = newLanguage;
            
            _instance.LoadStringsForCurrentLanguage();
            LanguageChanged?.Invoke();
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

        public static string GetStringById(LocalizedStringId id)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get String by Id before StringTable was available", GameComponent.Other);
                return null;
            }

            if (!_instance._stringsById.ContainsKey(id.Id))
                return "MISSING-STRING-" + id;

            return _instance._stringsById[id.Id];
        }

        public static bool IsReady()
        {
            return _instance != null;
        }
    }
}