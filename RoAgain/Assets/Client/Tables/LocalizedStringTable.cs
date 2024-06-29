using OwlLogging;
using Shared;
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

    public class LocalizedStringTable
    {
        private static LocalizedStringTable _instance;
        private static ClientLanguage _currentLanguage;
        public static event Action LanguageChanged;

        [Serializable]
        private class StringTableEntry
        {
            public string TextEnglish;
            public string TextGerman;
        }

        // Contains only the strings for the currently loaded language
        // This Dictionary will probably get split up if String-banks are ever being added
        // or LocalizedStringId becomes a more complex type than just a single int
        private Dictionary<int, string> _stringsById;

        private const string FILE_KEY = CachedFileAccess.CLIENT_DB_PREFIX + "LocStringTable";

        public void Register()
        {
            if (_instance != null)
            {
                if (_instance == this)
                    OwlLogger.LogError("Duplicate StringTable registration!", GameComponent.Other);
                else
                    OwlLogger.LogError($"Tried to register StringTable while another is registered", GameComponent.Other);
                return;
            }

            _instance = this;

            if(_currentLanguage != ClientLanguage.Unknown)
            {
                LoadStringsForCurrentLanguage();
            }
        }

        public static void SetClientLanguage(ClientLanguage newLanguage, bool forceReload = false)
        {
            if (_currentLanguage == newLanguage)
                return;

            if (_instance == null)
                return;

            _currentLanguage = newLanguage;
            
            _instance.LoadStringsForCurrentLanguage(forceReload);
            LanguageChanged?.Invoke();
        }

        private void LoadStringsForCurrentLanguage(bool forceReload = false)
        {
            if (forceReload)
            {
                CachedFileAccess.Load<DictionarySerializationWrapper<int, StringTableEntry>>(FILE_KEY, true);
            }

            var rawData = CachedFileAccess.GetOrLoad<DictionarySerializationWrapper<int, StringTableEntry>>(FILE_KEY, true);

            if (rawData == null)
            {
                OwlLogger.LogError("No Localization table available - file is likely missing!", GameComponent.Other);
                return;
            }
            Dictionary<int, StringTableEntry> allLangData = rawData.ToDict();

            _stringsById ??= new();

            _stringsById.Clear();
            foreach (KeyValuePair<int, StringTableEntry> kvp in allLangData)
            {
                string text = _currentLanguage switch
                {
                    ClientLanguage.English => kvp.Value.TextEnglish,
                    ClientLanguage.German => kvp.Value.TextGerman,
                    _ => null
                };
                if(string.IsNullOrEmpty(text))
                {
                    text = $"MISSING_TEXT_{_currentLanguage}_{kvp.Key}";
                }

                if(_stringsById.ContainsKey(kvp.Key))
                {
                    OwlLogger.LogError($"Duplicate LocalizedStringId: {kvp.Key}!", GameComponent.Other);
                }
                else
                {
                    _stringsById.Add(kvp.Key, text);
                }
            }

            CachedFileAccess.Purge(FILE_KEY);
        }

        public static void ReloadStrings()
        {
            if (_instance == null)
                return;

            _instance.LoadStringsForCurrentLanguage(true);
        }

        public static string GetStringById(LocalizedStringId id)
        {
            if (_instance == null)
            {
                OwlLogger.LogError("Tried to get String by Id before StringTable was available", GameComponent.Other);
                return null;
            }

            if (id == LocalizedStringId.INVALID)
                return "INVALID-LOCALIZED-STRING";

            if (!_instance._stringsById.ContainsKey(id.Id))
                return "MISSING-STRING-" + id;

            return _instance._stringsById[id.Id];
        }

        public static bool IsReady()
        {
            return _instance != null;
        }

        public static void Unregister()
        {
            if (_instance == null)
            {
                OwlLogger.LogWarning("Can't unregister LocalizedStringTable - none registered.", GameComponent.Other);
                return;
            }

            _instance = null;
        }
    }
}