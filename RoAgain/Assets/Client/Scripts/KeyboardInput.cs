using Client;
using OwlLogging;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardInput
{
    public static KeyboardInput Instance { get; private set; }

    public bool ChatMode = false; // disables inputs on keys which are defined as "chatbox-related"

    private static readonly HashSet<KeyCode> _chatKeys = new() {
        KeyCode.None
    };

    private Dictionary<KeyCode, HashSet<ConfigurableHotkey>> _hotkeyBanks = new();
    private HashSet<KeyCode> _usedModifierKeys = new();

    private ClientConfiguration _config;

    public int Initialize(ClientConfiguration config)
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

        foreach(var kvp in config.GetHotkeyConfig())
        {
            if(kvp.Value.Modifier != KeyCode.None)
            {
                _usedModifierKeys.Add(kvp.Value.Modifier);
                if(!_hotkeyBanks.ContainsKey(kvp.Value.Modifier))
                    _hotkeyBanks[kvp.Value.Modifier] = new();
            }
            _hotkeyBanks[kvp.Value.Modifier].Add(kvp.Key);
        }

        Instance = this;

        return 0;
    }

    public bool IsConfigurableHotkeyDown(ConfigurableHotkey hotkey)
    {
        ClientConfiguration.HotkeyConfigEntry entry = _config.GetHotkey(hotkey);
        if (entry == null)
            return false;

        bool mod = entry.Modifier != KeyCode.None ? Input.GetKey(entry.Modifier) : true;
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
