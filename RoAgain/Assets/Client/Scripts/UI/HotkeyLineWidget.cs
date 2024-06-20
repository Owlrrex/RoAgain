using OwlLogging;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class ConfigLineData
    {
        public ConfigKey Key;
        public HotkeyConfigEntry LocalValue;
        public HotkeyConfigEntry CharValue;
        public HotkeyConfigEntry AccValue;
    }  

    public class HotkeyLineWidget : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _keyText;

        [SerializeField]
        private HotkeyWidget _localHotkeyWidget;
        [SerializeField]
        private HotkeyWidget _accHotkeyWidget;
        [SerializeField]
        private HotkeyWidget _charHotkeyWidget;

        private ConfigLineData _data;

        private void Awake()
        {
            OwlLogger.PrefabNullCheckAndLog(_keyText, nameof(_keyText), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_localHotkeyWidget, nameof(_localHotkeyWidget), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_accHotkeyWidget, nameof(_accHotkeyWidget), this, GameComponent.UI);
            OwlLogger.PrefabNullCheckAndLog(_charHotkeyWidget, nameof(_charHotkeyWidget), this, GameComponent.UI);

            _localHotkeyWidget.ValueChanged += OnLocalValueChanged;
            _accHotkeyWidget.ValueChanged += OnAccValueChanged;
            _charHotkeyWidget.ValueChanged += OnCharValueChanged;
        }

        public void Init(ConfigKey configKey)
        {
            if(!configKey.IsHotkey())
            {
                OwlLogger.LogError($"Can't initialize HotkeyLineWidget with non-hotkey config key {configKey}!", GameComponent.UI);
                return;
            }

            ConfigLineData data = new ConfigLineData();
            data.Key = configKey;
            data.LocalValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Local) ?? new();
            data.AccValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Account) ?? new();
            data.CharValue = MixedConfiguration.Instance.GetHotkey(configKey, MixedConfigSource.Character) ?? new();

            Init(data);
        }

        public void Init(ConfigLineData data)
        {
            _data = data;
            UpdateKeyString();
            UpdateLocalHotkeyWidget();
            UpdateAccHotkeyWidget();
            UpdateCharHotkeyWidget();
        }

        private void UpdateKeyString()
        {
            _keyText.SetText(_data.Key.ToString());
        }

        private void UpdateLocalHotkeyWidget()
        {
            _localHotkeyWidget.SetValue(_data.LocalValue);
        }

        private void UpdateAccHotkeyWidget()
        {
            _accHotkeyWidget.SetValue(_data.AccValue);
        }

        private void UpdateCharHotkeyWidget()
        {
            _charHotkeyWidget.SetValue(_data.CharValue);
        }

        private void OnLocalValueChanged(HotkeyWidget _)
        {
            _data.LocalValue = _localHotkeyWidget.Value;

            UpdateLocalHotkeyWidget();

            MixedConfiguration.Instance.SetHotkey(_data.Key, _data.LocalValue, MixedConfigSource.Local);
        }

        private void OnAccValueChanged(HotkeyWidget _)
        {
            _data.AccValue = _accHotkeyWidget.Value;

            UpdateAccHotkeyWidget();

            MixedConfiguration.Instance.SetHotkey(_data.Key, _data.AccValue, MixedConfigSource.Account);
        }

        private void OnCharValueChanged(HotkeyWidget _)
        {
            _data.CharValue = _charHotkeyWidget.Value;

            UpdateCharHotkeyWidget();

            MixedConfiguration.Instance.SetHotkey(_data.Key, _data.CharValue, MixedConfigSource.Character);
        }
    }
}