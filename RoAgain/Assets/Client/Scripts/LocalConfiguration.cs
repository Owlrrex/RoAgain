using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Client.LocalConfiguration;
using LocalConfigPersistent = Shared.DictionarySerializationWrapper<Client.ConfigKey, int>;
using System.Text;

namespace Client
{
    [Serializable]
    public class HotkeyConfigEntry
    {
        public KeyCode Key = KeyCode.None;
        public KeyCode Modifier = KeyCode.None;

        public bool IsValid()
        {
            if (Key == KeyCode.None)
                return false;

            if (Modifier == Key)
                return false;

            return true; // Keys with and without modifier are always valid
        }

        public override string ToString()
        {
            if (Modifier != KeyCode.None)
            {
                return $"{Modifier.ToHotkeyString()} + {Key.ToHotkeyString()}";
            }
            else
            {
                return Key.ToHotkeyString();
            }
        }
    }

    public class LocalConfiguration
    {
        private const string LOCAL_FILE_KEY = CachedFileAccess.CONFIG_PREFIX + "MiscConfig";

        public static LocalConfiguration Instance { get; private set; }

        private Dictionary<ConfigKey, HotkeyConfigEntry> _hotkeyConfig = new();
        private Dictionary<ConfigKey, int> _miscConfig = new();

        public int LoadConfig()
        {
            if(Instance != null && Instance != this)
            {
                OwlLogger.LogError("Can't Load a second config object when one already exists - use existing instance!", GameComponent.Config);
                return -1;
            }

            bool changedAnyConfig = false;

            // Misc Config
            LocalConfigPersistent localPers = CachedFileAccess.GetOrLoad<LocalConfigPersistent>(LOCAL_FILE_KEY, false);
            if (localPers != null) // file did exist
            {
                _miscConfig = localPers.ToDict();
            }

            // Validate Misc Config
            changedAnyConfig |= FillInDefaultMiscConfig();

            // Populate Hotkey Config
            _hotkeyConfig.Clear();
            foreach (KeyValuePair<ConfigKey, int> kvp in _miscConfig)
            {
                if (!kvp.Key.IsHotkey())
                    continue;

                HotkeyConfigEntry entry = ((uint)kvp.Value).ToConfigurableHotkey();
                _hotkeyConfig.Add(kvp.Key, entry);
            }

            // Validate Hotkeys
            List<ConfigKey> invalidKeys = new();
            foreach (KeyValuePair<ConfigKey, HotkeyConfigEntry> kvp in _hotkeyConfig)
            {
                if (!kvp.Value.IsValid())
                    invalidKeys.Add(kvp.Key);
            }
            foreach (ConfigKey key in invalidKeys)
            {
                OwlLogger.LogError($"Hotkey Config for hotkey {key} was invalid and discarded!", GameComponent.Config);
                _hotkeyConfig.Remove(key);
            }

            changedAnyConfig |= FillInDefaultHotkeyConfig();

            if (changedAnyConfig)
            {
                SaveConfig();
            }

            CachedFileAccess.Purge(LOCAL_FILE_KEY);

            Instance = this;

            return 0;
        }

        private bool FillInDefaultHotkeyConfig()
        {
            bool anyChange = false;
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar11, new() { Key = KeyCode.Alpha1 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar12, new() { Key = KeyCode.Alpha2 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar13, new() { Key = KeyCode.Alpha3 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar14, new() { Key = KeyCode.Alpha4 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar15, new() { Key = KeyCode.Alpha5 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar16, new() { Key = KeyCode.Alpha6 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar17, new() { Key = KeyCode.Alpha7 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar18, new() { Key = KeyCode.Alpha8 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_Hotbar19, new() { Key = KeyCode.Alpha9 });
            // Hotkey10 currently not used - hotbar is 9 slots long

            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_ToggleCharMainWindow, new() { Key = KeyCode.V });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_ToggleStatWindow, new() { Key = KeyCode.A });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_ToggleSkillWindow, new() { Key = KeyCode.S });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_ToggleHotbar, new() { Key = KeyCode.F12 });
            anyChange |= _hotkeyConfig.TryAdd(ConfigKey.Hotkey_ToggleGameMenuWindow, new() { Key = KeyCode.Escape });

            return anyChange;
        }

        private bool FillInDefaultMiscConfig()
        {
            bool anyChange = false;

            anyChange |= _miscConfig.TryAdd(ConfigKey.ServerIp, (int)"127.0.0.1".ToIpAddressUint());
            anyChange |= _miscConfig.TryAdd(ConfigKey.ServerPort, 13337);
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

            foreach(KeyValuePair<ConfigKey, HotkeyConfigEntry> kvp in _hotkeyConfig)
            {
                _miscConfig[kvp.Key] = kvp.Value.ToInt();
            }

            LocalConfigPersistent localPers = new(_miscConfig);

            int miscResult = CachedFileAccess.Save(LOCAL_FILE_KEY, localPers);

            CachedFileAccess.Purge(LOCAL_FILE_KEY);

            if (miscResult != 0)
            {
                OwlLogger.LogError($"Saving of some configurations failed. miscresult = {miscResult}", GameComponent.Config);
                return -1;
            }

            return 0;
        }

        public HotkeyConfigEntry GetHotkey(ConfigKey hotkey)
        {
            if (!hotkey.IsHotkey())
            {
                OwlLogger.LogError($"Tried to get Hotkey for non-hotkey config {hotkey}!", GameComponent.Config);
                return null;
            }

            if (!_hotkeyConfig.ContainsKey(hotkey))
            {
                return null;
            }

            return _hotkeyConfig[hotkey];
        }

        public void SetHotkey(ConfigKey hotkey, HotkeyConfigEntry entry)
        {
            if (entry == null)
                return;

            _hotkeyConfig[hotkey] = entry;
        }

        public int GetConfig(ConfigKey key)
        {
            if (key.IsHotkey())
            {
                OwlLogger.LogError($"ConfigKey {key} is a Hotkey - should use GetHotkey() instead of GetConfig()!", GameComponent.Config);
            }

            if(!_miscConfig.ContainsKey(key))
            {
                return 0;
            }

            return _miscConfig[key];
        }

        public void SetConfig(ConfigKey key, int value)
        {
            if(key.IsHotkey())
            {
                OwlLogger.LogError($"ConfigKey {key} is a Hotkey - should use SetHotkey() instead, value {value} will be overwritten when saving!", GameComponent.Config);
            }

            _miscConfig[key] = value;
        }
    }

    public static class ConfigExtensions
    {
        public static int ToInt(this HotkeyConfigEntry hotkey)
        {
            if ((uint)hotkey.Modifier > ushort.MaxValue
                || (uint)hotkey.Key > ushort.MaxValue)
            {
                OwlLogger.LogError($"Can't convert configurable hotkey to int - key values are too big! Key = {hotkey.Key}, Modifier = {hotkey.Modifier}", GameComponent.Other);
                return 0;
            }

            ushort key = (ushort)hotkey.Key;
            ushort mod = (ushort)hotkey.Modifier;
            uint result = key + (uint)(mod << 16);
            return (int)result;
        }

        public static HotkeyConfigEntry ToConfigurableHotkey(this uint value)
        {
            uint tophalf = value & 0xFFFF0000;
            uint bothalf = value & 0x0000FFFF;
            ushort key = (ushort)bothalf;
            ushort mod = (ushort)(tophalf >> 16);
            return new() { Key = (KeyCode)key, Modifier = (KeyCode)mod };
        }

        public static uint ToIpAddressUint(this string ipAddress)
        {
            if (ipAddress == null)
            {
                return 0;
            }

            string[] parts = ipAddress.Split('.');
            if (parts.Length != 4)
            {
                return 0;
            }

            uint result = 0;
            byte value;
            if (!byte.TryParse(parts[0], out value))
            {
                return 0;
            }
            result += value;

            if (!byte.TryParse(parts[1], out value))
            {
                return 0;
            }
            result += (uint)(value << 8);

            if (!byte.TryParse(parts[2], out value))
            {
                return 0;
            }
            result += (uint)(value << 16);

            if (!byte.TryParse(parts[3], out value))
            {
                return 0;
            }
            result += (uint)(value << 24);

            return result;
        }
        
        public static string ToIpAddressString(this uint ipAddress)
        {
            StringBuilder builder = new();
            builder.Append(ipAddress & 0xFF);
            builder.Append(".");
            builder.Append((ipAddress & 0xFF00) >> 8);
            builder.Append(".");
            builder.Append((ipAddress & 0xFF0000) >> 16);
            builder.Append(".");
            builder.Append((ipAddress & 0xFF000000) >> 24);
            return builder.ToString();
        }

        public static bool IsHotkey(this ConfigKey key)
        {
            return key >= ConfigKey.Hotkey_BEGIN && key <= ConfigKey.Hotkey_END;
        }
    }

    public enum MixedConfigSource
    {
        Unknown,
        Local,
        Account,
        Character
    }

    public class MixedConfiguration
    {
        public static MixedConfiguration Instance;

        private RemoteConfigCache _remoteConfig;
        private LocalConfiguration _localConfig;

        public int Initialize(RemoteConfigCache remoteConfig, LocalConfiguration localConfig)
        {
            if(remoteConfig == null || localConfig == null)
            {
                OwlLogger.LogError("Can't initialize MixedConfiguration with null remote/local config!", GameComponent.Config);
                return -1;
            }

            if(Instance != null)
            {
                OwlLogger.LogError("Can't initialize MixedConfiguration when another already exists!", GameComponent.Config);
                return -2;
            }

            _remoteConfig = remoteConfig;
            _localConfig = localConfig;

            Instance = this;

            return 0;
        }

        public int GetConfigValue(ConfigKey key)
        {
            if(!IsLocalOnly(key) && _remoteConfig.TryGetConfigValueFallthrough(key, out int value))
                return value;

            return _localConfig.GetConfig(key);
        }

        public int GetConfigValue(ConfigKey key, MixedConfigSource source)
        {
            switch (source)
            {
                case MixedConfigSource.Local:
                    return _localConfig.GetConfig(key);
                case MixedConfigSource.Account:
                    if (_remoteConfig.TryGetAccConfigValue(key, out int value))
                        return value;
                    break;
                case MixedConfigSource.Character:
                    if (_remoteConfig.TryGetCharConfigValue(key, out int val))
                        return val;
                    break;
                default:
                    OwlLogger.LogError("Can't get config value from unknown Source!", GameComponent.Config);
                    break;
            }
            return 0;
        }

        public HotkeyConfigEntry GetHotkey(ConfigKey key)
        {
            if (!IsLocalOnly(key) && _remoteConfig.TryGetConfigValueFallthrough(key, out int value))
                return ((uint)value).ToConfigurableHotkey();

            return _localConfig.GetHotkey(key);
        }

        public HotkeyConfigEntry GetHotkey(ConfigKey key, MixedConfigSource source)
        {
            switch (source)
            {
                case MixedConfigSource.Local:
                    return _localConfig.GetHotkey(key);
                case MixedConfigSource.Account:
                    if (_remoteConfig.TryGetAccConfigValue(key, out int value))
                        return ((uint)value).ToConfigurableHotkey();
                    break;
                case MixedConfigSource.Character:
                    if (_remoteConfig.TryGetCharConfigValue(key, out int val))
                        return ((uint)val).ToConfigurableHotkey();
                    break;
                default:
                    OwlLogger.LogError("Can't get config value from unknown Source!", GameComponent.Config);
                    break;
            }
            return null;
        }

        public void SetConfigValue(ConfigKey key, int value, MixedConfigSource target)
        {
            switch(target)
            {
                case MixedConfigSource.Local:
                    _localConfig.SetConfig(key, value);
                    _localConfig.SaveConfig();
                    return;
                case MixedConfigSource.Account:
                    _remoteConfig.SaveAccountConfigValue(key, value);
                    return;
                case MixedConfigSource.Character:
                    _remoteConfig.SaveCharConfigValue(key, value);
                    return;
                default:
                    OwlLogger.LogError($"Can't save config value {key} = {value} for unknown target {target}!", GameComponent.Config);
                    return;
            }
        }

        public void SetHotkey(ConfigKey key, HotkeyConfigEntry entry, MixedConfigSource target)
        {
            switch (target)
            {
                case MixedConfigSource.Local:
                    _localConfig.SetHotkey(key, entry);
                    _localConfig.SaveConfig();
                    return;
                case MixedConfigSource.Account:
                    _remoteConfig.SaveAccountConfigValue(key, entry.ToInt());
                    return;
                case MixedConfigSource.Character:
                    _remoteConfig.SaveCharConfigValue(key, entry.ToInt());
                    return;
                default:
                    OwlLogger.LogError($"Can't save hotkey config {key} = {entry} for unknown target {target}!", GameComponent.Config);
                    return;
            }
        }

        public bool IsLocalOnly(ConfigKey key)
        {
            return key switch
            {
                ConfigKey.ServerIp => true,
                ConfigKey.ServerPort => true,
                _ => false
            };
        }

        public bool IsRemoteOnly(ConfigKey key)
        {
            return false;
        }

        public void FetchAccountSettings()
        {
            for (ConfigKey key = ConfigKey.Hotkey_BEGIN; key <= ConfigKey.Hotkey_END; key++)
            {
                _remoteConfig.FetchConfigValue(key, true);
            }
        }

        public void FetchCharacterSettings()
        {
            for (ConfigKey key = ConfigKey.Hotkey_BEGIN; key <= ConfigKey.Hotkey_END; key++)
            {
                _remoteConfig.FetchConfigValue(key, false);
            }

            for (ConfigKey key = ConfigKey.SkillData_Hotbar10; key <= ConfigKey.SkillData_Hotbar40; key++)
            {
                _remoteConfig.FetchConfigValue(key, false);
            }
        }

        public bool AnyRequestsPending()
        {
            return _remoteConfig.AnyRequestsPending();
        }
    }
}

