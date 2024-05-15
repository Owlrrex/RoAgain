using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

using HotkeyConfigPersistent = Shared.DictionarySerializationWrapper<Client.ConfigurableHotkey, Client.LocalConfiguration.HotkeyConfigEntry>;
using MiscConfigPersistent = Shared.DictionarySerializationWrapper<Client.ConfigurationKey, string>;

namespace Client
{
    public enum ConfigurableHotkey
    {
        Unknown,
        // Hotbar
        Hotbar1,
        Hotbar2,
        Hotbar3,
        Hotbar4,
        Hotbar5,
        Hotbar6,
        Hotbar7,
        Hotbar8,
        Hotbar9,
        Hotbar10,
        // Showing/Hiding/minimizing windows
        ToggleCharMainWindow,
        ToggleStatWindow,
        ToggleSkillWindow,
        ToggleHotbar,
        ToggleGameMenuWindow,
        // Chat
        ToggleChatInput,
        // General UI
        ConfirmDialog,

        // TODO
    }

    public enum ConfigurationKey
    {
        Unknown,
        TestMiscConfigEntry,
        ServerIp,
        ServerPort
        // Used for settings that aren't related to hotkeys, like audio Volume
    }

    public class LocalConfiguration
    {
        private const string HOTKEY_FILE_KEY = CachedFileAccess.CONFIG_PREFIX + "HotkeyConfig";
        private const string MISC_FILE_KEY = CachedFileAccess.CONFIG_PREFIX + "MiscConfig";

        public static LocalConfiguration Instance { get; private set; }

        [Serializable]
        public class HotkeyConfigEntry
        {
            public KeyCode Key = KeyCode.None;
            public KeyCode Modifier = KeyCode.None;

            public override string ToString()
            {
                string keyString = Key.ToHotkeyString();
                if (Modifier != KeyCode.None)
                {
                    keyString = $"{Modifier} + {keyString}";
                }
                return keyString;
            }
        }

        private Dictionary<ConfigurableHotkey, HotkeyConfigEntry> _hotkeyConfig = new();
        private Dictionary<ConfigurationKey, string> _miscConfig = new();
        

        public int LoadConfig()
        {
            if(Instance != null && Instance != this)
            {
                OwlLogger.LogError("Can't Load a second config object when one already exists - use existing instance!", GameComponent.Config);
                return -1;
            }

            bool changedAnyConfig = false;
            // Hotkeys
            HotkeyConfigPersistent hotkeyPers = CachedFileAccess.GetOrLoad<HotkeyConfigPersistent>(HOTKEY_FILE_KEY, false);
            if(hotkeyPers == null) // indicates file didn't exist
            {
                LoadDefaultHotkeyConfig();
                changedAnyConfig = true;
            }
            else
            {
                _hotkeyConfig = hotkeyPers.ToDict();
            }

            // Validate Hotkeys
            List<ConfigurableHotkey> invalidKeys = new();
            foreach (KeyValuePair<ConfigurableHotkey, HotkeyConfigEntry> kvp in _hotkeyConfig)
            {
                if(!IsHotkeyConfigValid(kvp.Value))
                    invalidKeys.Add(kvp.Key);
            }
            foreach(ConfigurableHotkey key in invalidKeys)
            {
                OwlLogger.LogError($"Hotkey Config for hotkey {key} was invalid and discarded!", GameComponent.Config);
                _hotkeyConfig.Remove(key);
            }

            changedAnyConfig |= FillInDefaultHotkeyConfig();

            // Misc Config
            MiscConfigPersistent miscPers = CachedFileAccess.GetOrLoad<MiscConfigPersistent>(MISC_FILE_KEY, false);
            if (miscPers == null) // indicates file didn't exist
            {
                LoadDefaultMiscConfig();
                changedAnyConfig = true;
            }
            else
            {
                _miscConfig = miscPers.ToDict();
            }

            // Validate Misc Config
            changedAnyConfig |= FillInDefaultMiscConfig();

            if(changedAnyConfig)
            {
                SaveConfig();
            }
            else
            {
                CachedFileAccess.Purge(HOTKEY_FILE_KEY);
                CachedFileAccess.Purge(MISC_FILE_KEY);
            }

            Instance = this;

            return 0;
        }

        public int LoadDefaultHotkeyConfig()
        {
            _hotkeyConfig.Clear();

            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar1, new() { Key = KeyCode.Alpha1 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar2, new() { Key = KeyCode.Alpha2 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar3, new() { Key = KeyCode.Alpha3 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar4, new() { Key = KeyCode.Alpha4 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar5, new() { Key = KeyCode.Alpha5 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar6, new() { Key = KeyCode.Alpha6 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar7, new() { Key = KeyCode.Alpha7 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar8, new() { Key = KeyCode.Alpha8 });
            _hotkeyConfig.Add(ConfigurableHotkey.Hotbar9, new() { Key = KeyCode.Alpha9 });
            // Hotkey10 currently not used - hotbar is 9 slots long

            _hotkeyConfig.Add(ConfigurableHotkey.ToggleCharMainWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.V });
            _hotkeyConfig.Add(ConfigurableHotkey.ToggleStatWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.A });
            _hotkeyConfig.Add(ConfigurableHotkey.ToggleSkillWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.S });
            _hotkeyConfig.Add(ConfigurableHotkey.ToggleHotbar, new() { Key = KeyCode.F12 });
            _hotkeyConfig.Add(ConfigurableHotkey.ToggleGameMenuWindow, new() { Key = KeyCode.Escape });

            return 0;
        }

        public bool FillInDefaultHotkeyConfig()
        {
            bool anyChange = false;
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar1, new() { Key = KeyCode.Alpha1 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar2, new() { Key = KeyCode.Alpha2 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar3, new() { Key = KeyCode.Alpha3 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar4, new() { Key = KeyCode.Alpha4 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar5, new() { Key = KeyCode.Alpha5 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar6, new() { Key = KeyCode.Alpha6 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar7, new() { Key = KeyCode.Alpha7 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar8, new() { Key = KeyCode.Alpha8 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.Hotbar9, new() { Key = KeyCode.Alpha9 });
            // Hotkey10 currently not used - hotbar is 9 slots long

            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.ToggleCharMainWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.V });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.ToggleStatWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.A });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.ToggleSkillWindow, new() { Modifier = KeyCode.LeftAlt, Key = KeyCode.S });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.ToggleHotbar, new() { Key = KeyCode.F12 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigurableHotkey.ToggleGameMenuWindow, new() { Key = KeyCode.Escape });
            return anyChange;
        }

        public int LoadDefaultMiscConfig()
        {
            _miscConfig.Clear();

            _miscConfig.Add(ConfigurationKey.TestMiscConfigEntry, "testValue1");
            _miscConfig.Add(ConfigurationKey.ServerIp, "127.0.0.1");
            _miscConfig.Add(ConfigurationKey.ServerPort, "13337");
            // Config entries here

            return 0;
        }

        public bool FillInDefaultMiscConfig()
        {
            bool anyChange = false;
            anyChange |= _miscConfig.TryAdd(ConfigurationKey.TestMiscConfigEntry, "testValue1");
            anyChange |= _miscConfig.TryAdd(ConfigurationKey.ServerIp, "127.0.0.1");
            anyChange |= _miscConfig.TryAdd(ConfigurationKey.ServerPort, "13337");
            // Config entries here

            return anyChange;
        }

        public int SaveConfig()
        {
            if((_hotkeyConfig == null || _hotkeyConfig.Count == 0)
                && (_miscConfig == null || _miscConfig.Count == 0))
            {
                return 0;
            }

            HotkeyConfigPersistent hotkeyPers = new(_hotkeyConfig);

            MiscConfigPersistent miscPers = new(_miscConfig);

            int hotkeyResult = CachedFileAccess.Save(HOTKEY_FILE_KEY, hotkeyPers);
            int miscResult = CachedFileAccess.Save(MISC_FILE_KEY, miscPers);

            CachedFileAccess.Purge(HOTKEY_FILE_KEY);
            CachedFileAccess.Purge(MISC_FILE_KEY);

            if (hotkeyResult != 0 || miscResult != 0)
            {
                OwlLogger.LogError($"Saving of some configurations failed. hotkeyResult = {hotkeyResult}, miscresult = {miscResult}", GameComponent.Other);
                return -1;
            }

            return 0;
        }

        private bool IsHotkeyConfigValid(HotkeyConfigEntry entry)
        {
            if (entry.Key == KeyCode.None)
                return false;

            if (entry.Modifier == entry.Key)
                return false;

            return true; // Keys with and without modifier are always valid
        }

        public int ResetConfig()
        {
            // TODO: Implement pending changes system
            // TODO: Discard any pending changes
            // TODO: Reload from disk?
            return -1;
        }

        public HotkeyConfigEntry GetHotkey(ConfigurableHotkey hotkey)
        {
            if(!_hotkeyConfig.ContainsKey(hotkey))
            {
                return null;
            }

            return _hotkeyConfig[hotkey];
        }

        public Dictionary<ConfigurableHotkey, HotkeyConfigEntry> GetHotkeyConfig()
        {
            return _hotkeyConfig;
        }

        public void SetHotkey(ConfigurableHotkey hotkey, HotkeyConfigEntry entry)
        {
            if (entry == null)
                return;

            _hotkeyConfig[hotkey] = entry;
        }

        public string GetMiscConfig(ConfigurationKey key)
        {
            if(!_miscConfig.ContainsKey(key))
            {
                return null;
            }

            return _miscConfig[key];
        }

        public void SetMiscConfig(ConfigurationKey key, string value)
        {
            if (value == null)
                return;

            _miscConfig[key] = value;
        }
    }
}

