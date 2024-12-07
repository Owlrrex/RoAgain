using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Client
{
    public class LocalizedStringTable : ILocalizedStringTable
    {
        private static LocalizationLanguage _currentLanguage;
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
        private Dictionary<int, string> _stringsById = new();

        private const string FILE_KEY = CachedFileAccess.CLIENT_DB_PREFIX + "LocStringTable";

        public void Register()
        {
            if (ILocalizedStringTable.Instance != null)
            {
                if (ILocalizedStringTable.Instance == this)
                    OwlLogger.LogError("Duplicate StringTable registration!", GameComponent.Other);
                else
                    OwlLogger.LogError($"Tried to register StringTable while another is registered", GameComponent.Other);
                return;
            }

            ILocalizedStringTable.Instance = this;

            if(_currentLanguage != LocalizationLanguage.Unknown)
            {
                LoadStringsForCurrentLanguage();
            }
        }

        public void SetClientLanguage(LocalizationLanguage newLanguage, bool forceReload = false)
        {
            if (_currentLanguage == newLanguage)
                return;

            _currentLanguage = newLanguage;

            LoadStringsForCurrentLanguage(forceReload);
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

            _stringsById.Clear();
            foreach (KeyValuePair<int, StringTableEntry> kvp in allLangData)
            {
                string text = _currentLanguage switch
                {
                    LocalizationLanguage.English => kvp.Value.TextEnglish,
                    LocalizationLanguage.German => kvp.Value.TextGerman,
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

        public void ReloadStrings()
        {
            LoadStringsForCurrentLanguage(true);
        }

        public string GetStringById(LocalizedStringId id)
        {
            if (!ILocalizedString.IsValid(id))
                return "INVALID-LOCALIZED-STRING";

            if (!_stringsById.ContainsKey(id.Id))
                return "MISSING-STRING-" + id;

            return _stringsById[id.Id];
        }

        public static bool IsReady()
        {
            return ILocalizedStringTable.Instance != null;
        }

        public static void Unregister()
        {
            if (ILocalizedStringTable.Instance == null)
            {
                OwlLogger.LogWarning("Can't unregister LocalizedStringTable - none registered.", GameComponent.Other);
                return;
            }

            ILocalizedStringTable.Instance = null;
        }
    }
}