using Client;
using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput
{
    public static KeyboardInput Instance { get; private set; }

    public bool ChatMode = false; // disables inputs on keys which are defined as "chatbox-related"

    private Dictionary<KeyCode, HashSet<ConfigKey>> _hotkeyBanks = new();
    private HashSet<KeyCode> _usedModifierKeys = new();

    private LocalConfiguration _config;

    public int Initialize(LocalConfiguration config)
    {
        if(Instance != null && Instance != this)
        {
            OwlLogger.LogError("Can't intialize a second instance of KeyboardInput - use the existing instance!", GameComponent.Input);
            return -1;
        }

        _hotkeyBanks.Clear();
        _usedModifierKeys.Clear();

        _config = config;

        _hotkeyBanks[KeyCode.None] = new();

        for(ConfigKey key = ConfigKey.Hotkey_BEGIN; key <= ConfigKey.Hotkey_END; key++)
        {
            HotkeyConfigEntry entry = LocalConfiguration.Instance.GetHotkey(key);
            if (entry == null)
                continue;

            if (entry.Modifier != KeyCode.None)
            {
                _usedModifierKeys.Add(entry.Modifier);
                if (!_hotkeyBanks.ContainsKey(entry.Modifier))
                    _hotkeyBanks[entry.Modifier] = new();
            }
            _hotkeyBanks[entry.Modifier].Add(key);
        }

        Instance = this;

        return 0;
    }

    public bool IsConfigurableHotkeyDown(ConfigKey hotkey)
    {
        HotkeyConfigEntry entry = _config.GetHotkey(hotkey);
        if (entry == null)
            return false;

        bool mod = entry.Modifier == KeyCode.None || Input.GetKey(entry.Modifier);
        bool key = Input.GetKeyDown(entry.Key);
        return mod && key;
    }

    // Function disabled because it allocates garbage on every call
    // Only re-enable if truly necessary for optimization

    //public List<ConfigurableHotkey> GetActiveHotkeys()
    //{
    //    List<ConfigurableHotkey> activeHotkeys = new();

    //    if(Input.anyKeyDown)
    //    {
    //        return activeHotkeys;
    //    }

    //    HashSet<ConfigurableHotkey> _currentHotkeyBank = _hotkeyBanks[KeyCode.None];
    //    foreach (KeyCode modifierKey in _usedModifierKeys)
    //    {
    //        if(Input.GetKey(modifierKey))
    //        {
    //            _currentHotkeyBank = _hotkeyBanks[modifierKey];
    //            break;
    //        }
    //    }

    //    foreach(ConfigurableHotkey hotkey in _currentHotkeyBank)
    //    {
    //        ClientConfiguration.HotkeyConfigEntry entry = _config.GetHotkey(hotkey);

    //        if (_chatKeys.Contains(entry.Modifier) && _chatKeys.Contains(entry.Key))
    //            continue;

    //        if(Input.GetKeyDown(entry.Key))
    //        {
    //            activeHotkeys.Add(hotkey);
    //        }
    //    }

    //    return activeHotkeys;
    //}
}
